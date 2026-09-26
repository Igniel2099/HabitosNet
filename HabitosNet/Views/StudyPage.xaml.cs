using HabitosNet.ViewModels;

namespace HabitosNet.Views;

public partial class StudyPage : ContentPage
{
    public StudyPage(StudyViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is StudyViewModel vm)
            await vm.InitializeAsync();
    }
}
