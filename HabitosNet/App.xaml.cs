using HabitosNet.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HabitosNet
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Truco de segundo plano sin servicios: congelar al minimizar
            // y recalcular por diferencia al volver (ahorro de batería).
            window.Stopped += (_, _) => ResolveStudyVm()?.HandleSleep();
            window.Resumed += (_, _) => ResolveStudyVm()?.HandleResume();

            return window;
        }

        private StudyViewModel? ResolveStudyVm()
        {
            try
            {
                return Handler?.MauiContext?.Services.GetService<StudyViewModel>();
            }
            catch
            {
                return null;
            }
        }
    }
}