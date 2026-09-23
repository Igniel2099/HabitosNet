using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Models;
using System.Globalization;

namespace HabitosNet.PageModels;

public partial class MainPageModel : ObservableObject, IProjectTaskPageModel
{
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly ModalErrorHandler _errorHandler;
    private readonly SeedDataService _seedDataService;
    private bool _dataLoaded;

    [ObservableProperty] private List<CategoryChartData> _todoCategoryData = [];
    [ObservableProperty] private List<Brush> _todoCategoryColors = [];
    [ObservableProperty] private List<ProjectTask> _tasks = [];
    [ObservableProperty] private List<ProjectTask> _visibleTasks = [];
    [ObservableProperty] private List<Project> _projects = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private bool _showCompleted;
    [ObservableProperty] private string _today = string.Empty;

    public int PendingCount => Tasks.Count(t => !t.IsCompleted);
    public int CompletedCount => Tasks.Count(t => t.IsCompleted);
    public int ProjectCount => Projects.Count;
    public bool HasCompletedTasks => CompletedCount > 0;
    public bool HasCategoryData => TodoCategoryData.Any(c => c.Count > 0);
    public string TaskListHint => ShowCompleted ? "Todas tus tareas, en un solo lugar." : "Lo que queda por hacer. Marca cada tarea al terminar.";
    public string EmptyMessage => Tasks.Count == 0 ? "Tu primera tarea empieza aquí" : "¡Todo al día!";
    public string EmptyHint => Tasks.Count == 0 ? "Crea un proyecto y añade una tarea para empezar." : "No tienes tareas pendientes. Puedes consultar las completadas.";

    public MainPageModel(SeedDataService seedDataService, ProjectRepository projectRepository,
        TaskRepository taskRepository, CategoryRepository categoryRepository, ModalErrorHandler errorHandler)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _categoryRepository = categoryRepository;
        _errorHandler = errorHandler;
        _seedDataService = seedDataService;
    }

    partial void OnShowCompletedChanged(bool value) => UpdateSummary();

    private void UpdateSummary()
    {
        VisibleTasks = Tasks.Where(t => ShowCompleted || !t.IsCompleted).OrderBy(t => t.IsCompleted).ToList();
        foreach (var property in new[] { nameof(PendingCount), nameof(CompletedCount), nameof(ProjectCount),
            nameof(HasCompletedTasks), nameof(TaskListHint), nameof(EmptyMessage), nameof(EmptyHint), nameof(HasCategoryData) })
            OnPropertyChanged(property);
    }

    private async Task LoadData()
    {
        Projects = await _projectRepository.ListAsync();
        Tasks = await _taskRepository.ListAsync();
        var categories = await _categoryRepository.ListAsync();
        var chartData = new List<CategoryChartData>();
        var chartColors = new List<Brush>();
        foreach (var category in categories)
        {
            int count = Projects.Where(p => p.CategoryID == category.ID).SelectMany(p => p.Tasks).Count(t => !t.IsCompleted);
            if (count == 0) continue;
            chartData.Add(new(category.Title, count));
            chartColors.Add(category.ColorBrush);
        }
        var knownCategories = categories.Select(c => c.ID).ToHashSet();
        int uncategorized = Projects.Where(p => !knownCategories.Contains(p.CategoryID))
            .SelectMany(p => p.Tasks).Count(t => !t.IsCompleted);
        if (uncategorized > 0)
        {
            chartData.Add(new("Sin categoría", uncategorized));
            chartColors.Add(new SolidColorBrush(Color.FromArgb("#778DA0")));
        }
        TodoCategoryData = chartData;
        TodoCategoryColors = chartColors;
        Today = DateTime.Today.ToString("dddd, d 'de' MMMM", CultureInfo.GetCultureInfo("es-ES"));
        UpdateSummary();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = IsRefreshing = true;
            await LoadData();
        }
        catch (Exception ex) { _errorHandler.HandleError(ex); }
        finally { IsBusy = IsRefreshing = false; }
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            if (!_dataLoaded)
            {
                if (!Preferences.Default.ContainsKey("is_seeded"))
                {
                    await _seedDataService.LoadSeedDataAsync();
                    Preferences.Default.Set("is_seeded", true);
                }
                _dataLoaded = true;
            }
            await LoadData();
        }
        catch (Exception ex) { _errorHandler.HandleError(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task TaskCompleted(ProjectTask task)
    {
        try
        {
            await _taskRepository.SaveItemAsync(task);
        }
        catch (Exception ex)
        {
            task.IsCompleted = !task.IsCompleted;
            UpdateSummary();
            _errorHandler.HandleError(ex);
            return;
        }
        await Refresh();
    }

    [RelayCommand] private Task AddTask() => Shell.Current.GoToAsync("task");
    [RelayCommand] private Task AddProject() => Shell.Current.GoToAsync("project");
    [RelayCommand] private Task ShowProjects() => Shell.Current.GoToAsync("//projects");
    [RelayCommand] private Task NavigateToTask(ProjectTask task) => Shell.Current.GoToAsync($"task?id={task.ID}");

    [RelayCommand]
    private async Task CleanTasks()
    {
        if (!await Shell.Current.DisplayAlertAsync("Eliminar tareas completadas",
            "Se eliminarán las tareas completadas de todos los proyectos. Esta acción no se puede deshacer.", "Eliminar", "Cancelar")) return;
        try
        {
            foreach (var task in Tasks.Where(t => t.IsCompleted).ToList())
                await _taskRepository.DeleteItemAsync(task);
            await LoadData();
            await AppShell.DisplayToastAsync("Tareas completadas eliminadas");
        }
        catch (Exception ex) { _errorHandler.HandleError(ex); }
    }
}
