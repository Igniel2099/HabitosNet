using System.Globalization;
using System.Text.RegularExpressions;

namespace HabitosNet.Pages
{
    public partial class ManageMetaPage : ContentPage
    {
        public ManageMetaPage(ManageMetaPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0 || PageScroll is null || FormLayout is null || OrganizationGrid is null || TagsCard is null)
                return;

            bool wide = Width >= 800;
            double inset = wide ? 24 : 16;
            PageScroll.Padding = new Thickness(inset);
            FormLayout.WidthRequest = Math.Min(920, Math.Max(0, Width - inset * 2));
            OrganizationGrid.ColumnDefinitions[1].Width = wide ? GridLength.Star : new GridLength(0);
            OrganizationGrid.ColumnSpacing = wide ? 16 : 0;
            Grid.SetRow(TagsCard, wide ? 0 : 1);
            Grid.SetColumn(TagsCard, wide ? 1 : 0);
        }
    }

    // A partially entered color is normal while typing; keep its preview neutral.
    public sealed class MetaColorPreviewConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var hex = value as string;
            return hex is not null && Regex.IsMatch(hex, "^#(?:[0-9a-fA-F]{3}){1,2}$")
                ? Color.FromArgb(hex)
                : Color.FromArgb("#CBD5E1");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
