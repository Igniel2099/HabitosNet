using CommunityToolkit.Mvvm.ComponentModel;

namespace HabitosNet.ViewModels;

public partial class DayCell : ObservableObject
{
    public DateOnly Date { get; }

    public string Title { get; }

    public bool IsToday { get; }

    [ObservableProperty]
    private bool isSelected;

    public DayCell(DateOnly date, string title, bool isToday, bool isSelected)
    {
        Date = date;
        Title = title;
        IsToday = isToday;
        IsSelected = isSelected;
    }
}
