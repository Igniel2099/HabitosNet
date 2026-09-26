namespace HabitosNet.ViewModels;

/// <summary>Una semana dentro del carrusel del histórico (siempre 7 días, Lun–Dom).</summary>
public class WeekItem
{
    public string Title { get; }

    public List<DaySummaryItem> Days { get; }

    public WeekItem(string title, List<DaySummaryItem> days)
    {
        Title = title;
        Days = days;
    }
}
