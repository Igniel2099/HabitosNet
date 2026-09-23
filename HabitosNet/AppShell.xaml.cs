using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Font = Microsoft.Maui.Font;

namespace HabitosNet
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            var savedTheme = Preferences.Default.Get("app_theme", string.Empty);
            var currentTheme = Enum.TryParse<AppTheme>(savedTheme, out var theme) ? theme : Application.Current!.RequestedTheme;
            ThemeSegmentedControl.SelectedIndex = currentTheme == AppTheme.Light ? 0 : 1;
            Application.Current!.UserAppTheme = currentTheme;
        }

        private void OnShellSizeChanged(object? sender, EventArgs e)
        {
            FlyoutBehavior = Width >= 1200 ? FlyoutBehavior.Locked : FlyoutBehavior.Flyout;
        }
        public static async Task DisplaySnackbarAsync(string message)
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#0C554F"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.Yellow,
                CornerRadius = new CornerRadius(12),
                Font = Font.SystemFontOfSize(16),
                ActionButtonFont = Font.SystemFontOfSize(14)
            };

            var snackbar = Snackbar.Make(message, visualOptions: snackbarOptions);

            await snackbar.Show(cancellationTokenSource.Token);
        }

        public static async Task DisplayToastAsync(string message)
        {
            // Windows uses an in-app snackbar, which doesn't require toast registration.
            if (OperatingSystem.IsWindows())
            {
                await DisplaySnackbarAsync(message);
                return;
            }

            var toast = Toast.Make(message, textSize: 18);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await toast.Show(cts.Token);
        }

        private void SfSegmentedControl_SelectionChanged(object? sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
        {
            Application.Current!.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark;
            Preferences.Default.Set("app_theme", Application.Current.UserAppTheme.ToString());
        }
    }
}
