namespace HabitosNet.Pages
{
    public partial class ProjectDetailPage : ContentPage
    {
        public ProjectDetailPage(ProjectDetailPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0 || PageScroll is null || FormLayout is null || DetailsGrid is null || OrganizationCard is null)
                return;

            // React to the window width as well as rotation on Android.
            bool wide = Width >= 800;
            double inset = wide ? 24 : 16;
            PageScroll.Padding = new Thickness(inset);
            FormLayout.WidthRequest = Math.Min(920, Math.Max(0, Width - inset * 2));
            DetailsGrid.ColumnDefinitions[1].Width = wide ? GridLength.Star : new GridLength(0);
            DetailsGrid.ColumnSpacing = wide ? 16 : 0;
            Grid.SetRow(OrganizationCard, wide ? 0 : 1);
            Grid.SetColumn(OrganizationCard, wide ? 1 : 0);
        }
    }
}
