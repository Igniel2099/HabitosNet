using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitosNet.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private bool _loading;
    private bool _initialized;

    [ObservableProperty]
    private ObservableCollection<DayCell> week = new();

    [ObservableProperty]
    private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private bool isToday = true;

    // Despertar
    [ObservableProperty]
    private TimeSpan? wakeTime;

    [ObservableProperty]
    private bool wakeDone;

    // Estirar
    [ObservableProperty]
    private bool stretchDone;

    // Leer
    [ObservableProperty]
    private int readTarget = 30;

    [ObservableProperty]
    private double readMinutes;

    [ObservableProperty]
    private bool readDone;

    [ObservableProperty]
    private string readPlaceholder = string.Empty;

    // Meditar
    [ObservableProperty]
    private int meditateTarget = 15;

    [ObservableProperty]
    private double meditateMinutes;

    [ObservableProperty]
    private bool meditateDone;

    [ObservableProperty]
    private string meditatePlaceholder = string.Empty;

    // Estudiar
    [ObservableProperty]
    private int studyTarget = 60;

    [ObservableProperty]
    private double studyMinutes;

    [ObservableProperty]
    private bool studyDone;

    [ObservableProperty]
    private string studyPlaceholder = string.Empty;

    public HomeViewModel(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;
        _initialized = true;

        await EnsurePastRecordsAsync();
        BuildWeek();
        await LoadDayAsync(SelectedDate);
    }

    private void BuildWeek()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Lunes de la semana actual (DayOfWeek: Sunday=0).
        int offset = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-offset);
        var culture = new CultureInfo("es-ES");

        Week.Clear();
        for (int i = 0; i < 7; i++)
        {
            var date = monday.AddDays(i);
            var title = date.ToString("ddd dd", culture).ToUpperInvariant();
            Week.Add(new DayCell(date, title, date == today, date == SelectedDate));
        }
    }

    [RelayCommand]
    private async Task SelectDayAsync(DayCell? cell)
    {
        if (cell is null)
            return;

        SelectedDate = cell.Date;
        foreach (var d in Week)
            d.IsSelected = d.Date == SelectedDate;

        await LoadDayAsync(SelectedDate);
    }

    private async Task LoadDayAsync(DateOnly date)
    {
        _loading = true;
        try
        {
            IsToday = date == DateOnly.FromDateTime(DateTime.Today);

            using var db = await _factory.CreateDbContextAsync();
            var reg = await db.DailyRegisters.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Date == date);

            WakeTime = reg?.WakeTime;
            WakeDone = reg?.WakeDone ?? false;
            StretchDone = reg?.StretchDone ?? false;

            ReadTarget = reg?.ReadTarget ?? 30;
            ReadMinutes = reg?.ReadActual ?? 0;
            ReadDone = reg?.ReadDone ?? false;

            MeditateTarget = reg?.MeditateTarget ?? 15;
            MeditateMinutes = reg?.MeditateActual ?? 0;
            MeditateDone = reg?.MeditateDone ?? false;

            StudyTarget = reg?.StudyTarget ?? 60;
            StudyMinutes = reg?.StudyActual ?? 0;
            StudyDone = reg?.StudyDone ?? false;

            // Placeholders con el tiempo real del día anterior.
            var prevDate = date.AddDays(-1);
            var prev = await db.DailyRegisters.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Date == prevDate);
            ReadPlaceholder = (prev?.ReadActual ?? ReadTarget).ToString();
            MeditatePlaceholder = (prev?.MeditateActual ?? MeditateTarget).ToString();
            StudyPlaceholder = (prev?.StudyActual ?? StudyTarget).ToString();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Garantiza que los días pasados tengan registro, pero siempre como NO completado.
    /// Si no registraste nada, nada queda completado: solo se heredan los objetivos.
    /// Solo crea fechas &lt; hoy y nunca sobrescribe lo ya guardado.
    /// </summary>
    private async Task EnsurePastRecordsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        using var db = await _factory.CreateDbContextAsync();

        for (int i = 14; i >= 1; i--)
        {
            var date = today.AddDays(-i);
            bool exists = await db.DailyRegisters.AnyAsync(r => r.Date == date);
            if (exists)
                continue;

            var prevDate = date.AddDays(-1);
            var prev = await db.DailyRegisters
                .FirstOrDefaultAsync(r => r.Date == prevDate);

            db.DailyRegisters.Add(new Models.DailyRegister
            {
                Date = date,
                WakeTime = null,
                WakeDone = false,
                StretchDone = false,
                ReadTarget = prev?.ReadTarget ?? 30,
                ReadActual = null,
                ReadDone = false,
                MeditateTarget = prev?.MeditateTarget ?? 15,
                MeditateActual = null,
                MeditateDone = false,
                StudyTarget = prev?.StudyTarget ?? 60,
                StudyActual = null,
                StudyDone = false,
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task SaveAsync()
    {
        if (_loading || !IsToday)
            return;

        // Si se marca completado sin tiempo, usar placeholder u objetivo.
        if (ReadDone && ReadMinutes <= 0 && int.TryParse(ReadPlaceholder, out int rp))
            ReadMinutes = rp;
        if (MeditateDone && MeditateMinutes <= 0 && int.TryParse(MeditatePlaceholder, out int mp))
            MeditateMinutes = mp;
        if (StudyDone && StudyMinutes <= 0 && int.TryParse(StudyPlaceholder, out int sp))
            StudyMinutes = sp;

        using var db = await _factory.CreateDbContextAsync();
        var reg = await db.DailyRegisters.FirstOrDefaultAsync(r => r.Date == SelectedDate);
        if (reg is null)
        {
            reg = new Models.DailyRegister { Date = SelectedDate };
            db.DailyRegisters.Add(reg);
        }

        reg.WakeTime = WakeTime;
        reg.WakeDone = WakeDone;
        reg.StretchDone = StretchDone;

        reg.ReadTarget = ReadTarget;
        reg.ReadActual = ReadMinutes > 0 ? (int)ReadMinutes : null;
        reg.ReadDone = ReadDone;

        reg.MeditateTarget = MeditateTarget;
        reg.MeditateActual = MeditateMinutes > 0 ? (int)MeditateMinutes : null;
        reg.MeditateDone = MeditateDone;

        reg.StudyTarget = StudyTarget;
        reg.StudyActual = StudyMinutes > 0 ? (int)StudyMinutes : null;
        reg.StudyDone = StudyDone;

        await db.SaveChangesAsync();
    }

    // Guardado automático ante cualquier cambio (solo si el día es hoy).
    partial void OnWakeTimeChanged(TimeSpan? value) => _ = SaveAsync();
    partial void OnWakeDoneChanged(bool value) => _ = SaveAsync();
    partial void OnStretchDoneChanged(bool value) => _ = SaveAsync();
    partial void OnReadMinutesChanged(double value) => _ = SaveAsync();
    partial void OnReadDoneChanged(bool value) => _ = SaveAsync();
    partial void OnMeditateMinutesChanged(double value) => _ = SaveAsync();
    partial void OnMeditateDoneChanged(bool value) => _ = SaveAsync();
    partial void OnStudyMinutesChanged(double value) => _ = SaveAsync();
    partial void OnStudyDoneChanged(bool value) => _ = SaveAsync();
}
