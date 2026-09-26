using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitosNet.ViewModels;

/// <summary>
/// Pomodoro inteligente: Trabajo -> Corto x3 -> Largo, con ciclo de 4.
/// Sin servicios en segundo plano: en sleep se guarda ExpectedEndTime
/// y al volver se recalcula por diferencia (ahorro de batería).
/// </summary>
public partial class StudyViewModel : ObservableObject
{
    private const string PrefPomodoro = "pomodoro_minutes";
    private const string PrefShort = "short_minutes";
    private const string PrefLong = "long_minutes";

    private readonly IDbContextFactory<AppDbContext> _factory;
    private IDispatcherTimer? _timer;
    private DateTime _expectedEnd;
    private bool _wasRunning;
    private bool _initialized;
    private int _completedPomodoros;

    [ObservableProperty]
    private StudyMode mode = StudyMode.Pomodoro;

    [ObservableProperty]
    private TimeSpan remaining = TimeSpan.FromMinutes(25);

    [ObservableProperty]
    private string timeText = "25:00";

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private bool isPomodoro = true;

    [ObservableProperty]
    private bool isShort;

    [ObservableProperty]
    private bool isLong;

    [ObservableProperty]
    private string actionText = "▶ Empezar";

    [ObservableProperty]
    private string cycleText = "Ciclo 1/4";

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private int pomodoroMinutes = Preferences.Get(PrefPomodoro, 25);

    [ObservableProperty]
    private int shortMinutes = Preferences.Get(PrefShort, 5);

    [ObservableProperty]
    private int longMinutes = Preferences.Get(PrefLong, 15);

    [ObservableProperty]
    private int todayStudyMinutes;

    [ObservableProperty]
    private int todayStudyTarget = 60;

    public StudyViewModel(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
        UpdateDerived();
    }

    public async Task InitializeAsync()
    {
        await LoadTodayAsync();
        if (_initialized)
            return;
        _initialized = true;
        ResetRemaining();
    }

    private int ModeMinutes(StudyMode m) => m switch
    {
        StudyMode.Pomodoro => PomodoroMinutes,
        StudyMode.ShortBreak => ShortMinutes,
        StudyMode.LongBreak => LongMinutes,
        _ => PomodoroMinutes
    };

    private string ModeName(StudyMode m) => m switch
    {
        StudyMode.Pomodoro => "Pomodoro",
        StudyMode.ShortBreak => "Descanso corto",
        StudyMode.LongBreak => "Descanso largo",
        _ => string.Empty
    };

    private void UpdateDerived()
    {
        TimeText = $"{(int)Remaining.TotalMinutes:00}:{Remaining.Seconds:00}";
        ActionText = IsRunning ? "⏸ Pausar" : "▶ Empezar";
        IsPomodoro = Mode == StudyMode.Pomodoro;
        IsShort = Mode == StudyMode.ShortBreak;
        IsLong = Mode == StudyMode.LongBreak;
        CycleText = $"Ciclo {(_completedPomodoros % 4) + 1}/4";

        double total = Math.Max(1, ModeMinutes(Mode) * 60);
        Progress = 1.0 - (Remaining.TotalSeconds / total);
    }

    private void ResetRemaining()
    {
        Remaining = TimeSpan.FromMinutes(Math.Max(1, ModeMinutes(Mode)));
        UpdateDerived();
    }

    private void EnsureTimer()
    {
        if (_timer is not null)
            return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => OnTick();
    }

    private void OnTick()
    {
        if (!IsRunning)
            return;

        Remaining -= TimeSpan.FromSeconds(1);
        if (Remaining <= TimeSpan.Zero)
        {
            Remaining = TimeSpan.Zero;
            UpdateDerived();
            _ = FinishPhaseAsync();
            return;
        }
        UpdateDerived();
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (IsRunning)
        {
            Pause();
            return;
        }

        if (Remaining <= TimeSpan.Zero)
            ResetRemaining();

        EnsureTimer();
        IsRunning = true;
        _timer?.Start();
        UpdateDerived();
    }

    private void Pause()
    {
        _timer?.Stop();
        IsRunning = false;
        UpdateDerived();
    }

    [RelayCommand]
    private void Stop()
    {
        _timer?.Stop();
        IsRunning = false;
        ResetRemaining();
    }

    [RelayCommand]
    private void SelectMode(string modeName)
    {
        var next = modeName switch
        {
            "Short" => StudyMode.ShortBreak,
            "Long" => StudyMode.LongBreak,
            _ => StudyMode.Pomodoro
        };

        _timer?.Stop();
        IsRunning = false;
        Mode = next;
        ResetRemaining();
    }

    private async Task FinishPhaseAsync()
    {
        _timer?.Stop();
        IsRunning = false;

        if (Mode == StudyMode.Pomodoro)
        {
            await AddStudyMinutesAsync(PomodoroMinutes);
            _completedPomodoros++;
            Mode = _completedPomodoros % 4 == 0 ? StudyMode.LongBreak : StudyMode.ShortBreak;
        }
        else
        {
            Mode = StudyMode.Pomodoro;
        }

        ResetRemaining();
        await LoadTodayAsync();

        // Encadenado automático del ciclo.
        EnsureTimer();
        IsRunning = true;
        _timer?.Start();
        UpdateDerived();
    }

    private async Task AddStudyMinutesAsync(int minutes)
    {
        using var db = await _factory.CreateDbContextAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var reg = await db.DailyRegisters.FirstOrDefaultAsync(r => r.Date == today);
        if (reg is null)
        {
            reg = new Models.DailyRegister { Date = today };
            db.DailyRegisters.Add(reg);
        }

        reg.StudyActual = (reg.StudyActual ?? 0) + minutes;
        if (reg.StudyTarget <= 0)
            reg.StudyTarget = 60;
        if (reg.StudyActual >= reg.StudyTarget)
            reg.StudyDone = true;

        await db.SaveChangesAsync();
    }

    public async Task LoadTodayAsync()
    {
        using var db = await _factory.CreateDbContextAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var reg = await db.DailyRegisters.AsNoTracking().FirstOrDefaultAsync(r => r.Date == today);
        TodayStudyMinutes = reg?.StudyActual ?? 0;
        TodayStudyTarget = reg?.StudyTarget is > 0 ? reg.StudyTarget : 60;
    }

    /// <summary>App minimizada: congelar y anotar fin esperado.</summary>
    public void HandleSleep()
    {
        if (!IsRunning)
        {
            _wasRunning = false;
            return;
        }
        _wasRunning = true;
        _expectedEnd = DateTime.Now + Remaining;
        _timer?.Stop();
        IsRunning = false;
        UpdateDerived();
    }

    /// <summary>App reabierta: recalcular por diferencia.</summary>
    public void HandleResume()
    {
        if (!_wasRunning)
            return;
        _wasRunning = false;

        var diff = _expectedEnd - DateTime.Now;
        if (diff <= TimeSpan.Zero)
        {
            Remaining = TimeSpan.Zero;
            UpdateDerived();
            _ = FinishPhaseAsync();
            return;
        }

        Remaining = diff;
        EnsureTimer();
        IsRunning = true;
        _timer?.Start();
        UpdateDerived();
    }

    partial void OnModeChanged(StudyMode value) => UpdateDerived();
    partial void OnRemainingChanged(TimeSpan value) => UpdateDerived();
    partial void OnIsRunningChanged(bool value) => UpdateDerived();

    partial void OnPomodoroMinutesChanged(int value)
    {
        Preferences.Set(PrefPomodoro, Math.Max(1, value));
        if (!IsRunning && Mode == StudyMode.Pomodoro)
            ResetRemaining();
    }

    partial void OnShortMinutesChanged(int value)
    {
        Preferences.Set(PrefShort, Math.Max(1, value));
        if (!IsRunning && Mode == StudyMode.ShortBreak)
            ResetRemaining();
    }

    partial void OnLongMinutesChanged(int value)
    {
        Preferences.Set(PrefLong, Math.Max(1, value));
        if (!IsRunning && Mode == StudyMode.LongBreak)
            ResetRemaining();
    }
}
