namespace HabitosNet.Pages;

public partial class MainPage : ContentPage
{
    private bool _showSummary;

    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateLayout();

    private void OnSummaryClicked(object? sender, EventArgs e)
    {
        _showSummary = !_showSummary;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (OverviewGrid is null) return;
        bool wide = Width >= 900;
        OverviewGrid.ColumnDefinitions = wide
            ? new ColumnDefinitionCollection(new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)), new ColumnDefinition(GridLength.Star))
            : new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star));
        Grid.SetColumn(SummaryPanel, wide ? 1 : 0);
        Grid.SetRow(SummaryPanel, wide ? 0 : 1);
        SummaryPanel.IsVisible = wide || _showSummary;
        SummaryToggle.IsVisible = !wide;
        SummaryToggle.Text = _showSummary ? "Ocultar resumen" : "Ver resumen por categoría";
    }
}
