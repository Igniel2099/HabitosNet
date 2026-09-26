using CommunityToolkit.Mvvm.ComponentModel;

namespace HabitosNet.ViewModels;

/// <summary>
/// Un día dentro de la tarjeta semanal del histórico.
/// IsExpanded lo lee el Expander (TwoWay) para abrir/cerrar el acordeón.
/// </summary>
public partial class DaySummaryItem : ObservableObject
{
    public DateOnly Date { get; }

    public string DayName { get; }

    public string QuickSummary { get; }

    public bool HasData { get; }

    public List<string> Details { get; }

    [ObservableProperty]
    private bool isExpanded;

    public DaySummaryItem(
        DateOnly date,
        string dayName,
        string quickSummary,
        bool hasData,
        List<string> details,
        bool isExpanded)
    {
        Date = date;
        DayName = dayName;
        QuickSummary = quickSummary;
        HasData = hasData;
        Details = details;
        IsExpanded = isExpanded;
    }
}
