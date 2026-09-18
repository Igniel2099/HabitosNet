using HabitosNet.Models;
using HabitosNet.PageModels;

namespace HabitosNet.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}