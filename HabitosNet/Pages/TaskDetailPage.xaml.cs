namespace HabitosNet.Pages
{
    public partial class TaskDetailPage : ContentPage
    {
        public TaskDetailPage(TaskDetailPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0 || PageScroll is null || FormLayout is null)
                return;

            double inset = Width >= 800 ? 24 : 16;
            PageScroll.Padding = new Thickness(inset);
            FormLayout.WidthRequest = Math.Min(920, Math.Max(0, Width - inset * 2));
        }
    }
}
