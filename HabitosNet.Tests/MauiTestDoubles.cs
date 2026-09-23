// Only presentation/platform APIs are replaced. The repositories, models, and
// SQL executed by these tests are the application sources linked by the project.
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Graphics;
global using HabitosNet.Data;
global using HabitosNet.Services;
global using HabitosNet.Utilities;
global using Fonts;

public static class FileSystem
{
    public static string AppDataDirectory { get; set; } = string.Empty;
    public static string PackageFileContents { get; set; } = string.Empty;

    public static Task<Stream> OpenAppPackageFileAsync(string filename) =>
        Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(PackageFileContents)));
}

namespace Microsoft.Maui.Controls
{
    public interface IQueryAttributable
    {
        void ApplyQueryAttributes(IDictionary<string, object> query);
    }

    public sealed class ShellNavigationQueryParameters : Dictionary<string, object> { }

    public sealed record NavigationRequest(string Route, IDictionary<string, object> Parameters);

    public sealed class Shell
    {
        public static Shell Current { get; set; } = new();
        public List<NavigationRequest> Navigations { get; } = [];
        public bool ConfirmNextAlert { get; set; } = true;
        public Func<Task>? BeforeNavigation { get; set; }

        public async Task GoToAsync(string route, ShellNavigationQueryParameters? parameters = null)
        {
            if (BeforeNavigation is not null)
                await BeforeNavigation();
            Navigations.Add(new(route, parameters ?? new()));
        }

        public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel) => Task.FromResult(ConfirmNextAlert);
        public Task DisplayAlertAsync(string title, string message, string cancel) => Task.CompletedTask;
    }

    public class Brush { }
    public sealed class SolidColorBrush(Color color) : Brush
    {
        public Color Color { get; } = color;
    }
}

public static class SemanticScreenReader
{
    public static void Announce(string message) { }
}

public sealed class Preferences
{
    public static Preferences Default { get; set; } = new();
    private readonly Dictionary<string, bool> _values = [];
    public void Set(string key, bool value) => _values[key] = value;
    public bool Get(string key, bool defaultValue) => _values.GetValueOrDefault(key, defaultValue);
}

namespace HabitosNet
{
    public static class AppShell
    {
        public static List<string> Toasts { get; } = [];
        public static Task DisplayToastAsync(string message)
        {
            Toasts.Add(message);
            return Task.CompletedTask;
        }
    }
}

namespace Microsoft.Maui.Graphics
{
    public sealed class Color
    {
        public static Color FromArgb(string value) => new();
    }
}

namespace CommunityToolkit.Maui.Core.Extensions
{
    public static class ColorExtensions
    {
        public static Color WithBlackKey(this Color color, double value) => color;
    }
}
