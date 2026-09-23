namespace HabitosNet.Pages;

public partial class ProjectListPage : ContentPage
{
    public ProjectListPage(ProjectListPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (ProjectLayout is not null)
            ProjectLayout.Span = Width >= 1100 ? 3 : Width >= 720 ? 2 : 1;
    }
}
