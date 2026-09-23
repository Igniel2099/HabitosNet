using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Models;

namespace HabitosNet.PageModels;

public partial class ProjectListPageModel : ObservableObject
{
    private readonly ProjectRepository _projectRepository;
    private readonly ModalErrorHandler _errorHandler;
    private List<Project> _allProjects = [];

    [ObservableProperty] private List<Project> _projects = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public string ResultSummary => $"{Projects.Count} proyecto{(Projects.Count == 1 ? "" : "s")}";
    public string EmptyTitle => string.IsNullOrWhiteSpace(SearchText) ? "Dale forma a tu próximo objetivo" : "No hay coincidencias";
    public string EmptyDescription => string.IsNullOrWhiteSpace(SearchText)
        ? "Crea un proyecto y divídelo en tareas pequeñas." : "Prueba con otro nombre o borra la búsqueda.";

    public ProjectListPageModel(ProjectRepository projectRepository, ModalErrorHandler errorHandler)
    {
        _projectRepository = projectRepository;
        _errorHandler = errorHandler;
    }

    partial void OnSearchTextChanged(string value) => FilterProjects();

    private void FilterProjects()
    {
        var term = (SearchText ?? string.Empty).Trim();
        Projects = _allProjects.Where(p => p.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || p.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase)).ToList();
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyDescription));
    }

    [RelayCommand]
    private async Task Appearing()
    {
        try
        {
            IsBusy = true;
            _allProjects = await _projectRepository.ListAsync();
            FilterProjects();
        }
        catch (Exception ex) { _errorHandler.HandleError(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private Task NavigateToProject(Project project) => Shell.Current.GoToAsync($"project?id={project.ID}");
    [RelayCommand] private Task AddProject() => Shell.Current.GoToAsync("project");
}
