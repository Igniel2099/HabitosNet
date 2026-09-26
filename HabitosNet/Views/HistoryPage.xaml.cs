using HabitosNet.ViewModels;

namespace HabitosNet.Views;

public partial class HistoryPage : ContentPage
{
    public HistoryPage(HistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HistoryViewModel vm)
            await vm.RefreshAsync();
    }
}
