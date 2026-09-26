using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Data;
using HabitosNet.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitosNet.ViewModels;

/// <summary>
/// Histórico de solo lectura: una semana visible con acordeón por día.
/// Sin CarouselView (frágil en WinUI vía Shell): navegación con ◀ ▶.
/// El día actual (o el último con datos) aparece abierto por defecto.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private const int PastWeeks = 12;

    private readonly IDbContextFactory<AppDbContext> _factory;
    private List<WeekItem> _weeks = new();
    private int _weekIndex;

    [ObservableProperty]
    private WeekItem? currentWeek;

    [ObservableProperty]
    private bool canGoPrev;

    [ObservableProperty]
    private bool canGoNext;

    public HistoryViewModel(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => RefreshAsync();

    public async Task RefreshAsync()
    {
        var culture = new CultureInfo("es-ES");
        var today = DateOnly.FromDateTime(DateTime.Today);
        int offset = (((int)today.DayOfWeek + 6) % 7);
        var thisMonday = today.AddDays(-offset);
        var firstMonday = thisMonday.AddDays(-7 * PastWeeks);
        var lastSunday = thisMonday.AddDays(6);

        using var db = await _factory.CreateDbContextAsync();
        var regs = await db.DailyRegisters.AsNoTracking()
            .Where(r => r.Date >= firstMonday && r.Date <= lastSunday)
            .ToDictionaryAsync(r => r.Date);

        var list = new List<WeekItem>();
        for (int w = 0; w <= PastWeeks; w++)
        {
            var monday = firstMonday.AddDays(w * 7);
            var sunday = monday.AddDays(6);
            var title = $"{monday:dd} – {sunday:dd} {sunday.ToString("MMM", culture).TrimEnd('.').ToUpperInvariant()}";

            var days = new List<DaySummaryItem>();
            for (int i = 0; i < 7; i++)
            {
                var date = monday.AddDays(i);
                regs.TryGetValue(date, out DailyRegister? reg);
                days.Add(BuildDay(date, reg, culture));
            }

            // UX: en la semana actual, abrir hoy (o el último día con datos).
            if (w == PastWeeks)
            {
                var target = days.FirstOrDefault(d => d.Date == today && d.HasData)
                    ?? days.LastOrDefault(d => d.Date <= today && d.HasData)
                    ?? days.FirstOrDefault(d => d.Date == today);
                if (target is not null)
                    target.IsExpanded = true;
            }

            list.Add(new WeekItem(title, days));
        }

        _weeks = list;
        _weekIndex = list.Count - 1;
        UpdateCurrentWeek();
    }

    [RelayCommand]
    private void PrevWeek()
    {
        if (_weekIndex <= 0)
            return;
        _weekIndex--;
        UpdateCurrentWeek();
    }

    [RelayCommand]
    private void NextWeek()
    {
        if (_weekIndex >= _weeks.Count - 1)
            return;
        _weekIndex++;
        UpdateCurrentWeek();
    }

    private void UpdateCurrentWeek()
    {
        CurrentWeek = _weeks.Count == 0 ? null : _weeks[_weekIndex];
        CanGoPrev = _weekIndex > 0;
        CanGoNext = _weekIndex < _weeks.Count - 1;
    }

    private static DaySummaryItem BuildDay(DateOnly date, DailyRegister? reg, CultureInfo culture)
    {
        var dayName = $"{culture.TextInfo.ToTitleCase(date.ToString("dddd", culture))} {date.Day}";

        if (reg is null)
        {
            return new DaySummaryItem(
                date, dayName, "Sin datos", false,
                new List<string> { "Sin registro para este día." }, false);
        }

        int done = (reg.WakeDone ? 1 : 0)
            + (reg.StretchDone ? 1 : 0)
            + (reg.ReadDone ? 1 : 0)
            + (reg.MeditateDone ? 1 : 0)
            + (reg.StudyDone ? 1 : 0);

        var details = new List<string>
        {
            $"⏰ Despertar {(reg.WakeTime is null ? "—" : reg.WakeTime.Value.ToString(@"hh\:mm"))} {(reg.WakeDone ? "✓" : "—")}",
            $"🧘 Estirar {(reg.StretchDone ? "✓" : "—")}",
            $"📖 Leer {reg.ReadActual?.ToString() ?? "—"}/{reg.ReadTarget} min {(reg.ReadDone ? "✓" : "—")}",
            $"🧠 Meditar {reg.MeditateActual?.ToString() ?? "—"}/{reg.MeditateTarget} min {(reg.MeditateDone ? "✓" : "—")}",
            $"📚 Estudiar {reg.StudyActual?.ToString() ?? "—"}/{reg.StudyTarget} min {(reg.StudyDone ? "✓" : "—")}",
        };

        return new DaySummaryItem(
            date, dayName, $"{done}/5 tareas", true, details, false);
    }
}
