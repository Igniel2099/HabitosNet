using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Models;

namespace HabitosNet.PageModels
{
    public partial class TaskDetailPageModel : ObservableObject, IQueryAttributable
    {
        public const string ProjectQueryKey = "project";
        public const string TaskQueryKey = "taskDraft";
        private ProjectTask? _task;
        private Project? _sourceProject;
        private Project? _draftProject;
        private bool _canDelete;
        private bool _isLoaded;
        private readonly ProjectRepository _projectRepository;
        private readonly TaskRepository _taskRepository;
        private readonly ModalErrorHandler _errorHandler;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isCompleted;

        [ObservableProperty]
        private List<Project> _projects = [];

        [ObservableProperty]
        private Project? _project;

        [ObservableProperty]
        private int _selectedProjectIndex = -1;

        [ObservableProperty]
        private bool _isExistingProject;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
        private string _validationMessage = string.Empty;

        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
        public bool CanSave => !IsBusy && _isLoaded;

        public bool CanDelete
        {
            get => _canDelete && !IsBusy && _isLoaded;
            private set
            {
                SetProperty(ref _canDelete, value);
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanDelete));
            SaveCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        public TaskDetailPageModel(ProjectRepository projectRepository, TaskRepository taskRepository, ModalErrorHandler errorHandler)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
            _errorHandler = errorHandler;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
            => LoadTaskAsync(query).FireAndForgetSafeAsync(_errorHandler);

        private async Task LoadTaskAsync(IDictionary<string, object> query)
        {
            _isLoaded = false;
            IsBusy = true;
            ValidationMessage = string.Empty;
            Title = string.Empty;
            IsCompleted = false;
            Projects = [];
            Project = null;
            SelectedProjectIndex = -1;
            IsExistingProject = true;
            CanDelete = false;
            _task = null;
            _sourceProject = query.TryGetValue(ProjectQueryKey, out var project) ? project as Project : null;
            _draftProject = _sourceProject?.ID == 0 ? _sourceProject : null;

            try
            {
                if (query.TryGetValue(TaskQueryKey, out var draft) && draft is ProjectTask draftTask)
                {
                    if (_sourceProject is null || !_sourceProject.Tasks.Contains(draftTask))
                    {
                        ValidationMessage = "No se encontró la tarea en el borrador del proyecto.";
                        return;
                    }

                    _draftProject = _sourceProject;
                    _task = draftTask;
                    CanDelete = true;
                }
                else if (query.TryGetValue("id", out var id))
                {
                    _task = await _taskRepository.GetAsync(Convert.ToInt32(id));
                    if (_task is null)
                    {
                        ValidationMessage = "No se encontró la tarea. Vuelve al listado y actualízalo.";
                        return;
                    }

                    CanDelete = true;
                }
                else
                {
                    _task = new ProjectTask { ProjectID = _sourceProject?.ID ?? 0 };
                }

                IsExistingProject = _draftProject is null;
                if (_draftProject is not null)
                {
                    Project = _draftProject;
                }
                else
                {
                    Projects = await _projectRepository.ListAsync();
                    SelectedProjectIndex = Projects.FindIndex(p => p.ID == _task.ProjectID);
                    Project = SelectedProjectIndex >= 0 ? Projects[SelectedProjectIndex] : null;
                    if (Projects.Count == 0)
                        ValidationMessage = "Crea primero un proyecto para poder guardar tareas.";
                }

                Title = _task.Title;
                IsCompleted = _task.IsCompleted;
                _isLoaded = true;
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo cargar la tarea. Vuelve atrás e inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            ValidationMessage = string.Empty;
            if (_task is null || !_isLoaded)
            {
                ValidationMessage = "No se pudo cargar la tarea. Vuelve atrás e inténtalo de nuevo.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ValidationMessage = "Escribe un nombre para la tarea.";
                return;
            }

            var destination = _draftProject ??
                (SelectedProjectIndex >= 0 && SelectedProjectIndex < Projects.Count ? Projects[SelectedProjectIndex] : null);
            if (destination is null || (_draftProject is null && destination.ID <= 0))
            {
                ValidationMessage = "Selecciona un proyecto antes de guardar la tarea.";
                return;
            }

            IsBusy = true;
            try
            {
                // Write a copy first so a failed write does not alter the previous screen's task.
                var savedTask = new ProjectTask
                {
                    ID = _task.ID,
                    Title = Title.Trim(),
                    IsCompleted = IsCompleted,
                    ProjectID = destination.ID
                };

                if (_draftProject is null)
                    await _taskRepository.SaveItemAsync(savedTask);

                _task.ID = savedTask.ID;
                _task.Title = savedTask.Title;
                _task.IsCompleted = savedTask.IsCompleted;
                _task.ProjectID = savedTask.ProjectID;

                if (_sourceProject is not null)
                {
                    _sourceProject.Tasks.RemoveAll(t => ReferenceEquals(t, _task) || (_task.ID > 0 && t.ID == _task.ID));
                    if (_sourceProject.ID == destination.ID)
                        _sourceProject.Tasks.Add(_task);
                }

                await Shell.Current.GoToAsync("..?refresh=true");
                await AppShell.DisplayToastAsync(_draftProject is null ? "Tarea guardada" : "Tarea añadida al borrador");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo guardar la tarea. Tus cambios siguen aquí; inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task Delete()
        {
            if (_task is null)
                return;

            IsBusy = true;
            ValidationMessage = string.Empty;
            try
            {
                if (!await Shell.Current.DisplayAlertAsync("Eliminar tarea", "¿Quieres eliminar esta tarea?", "Eliminar", "Cancelar"))
                    return;

                if (_task.ID > 0)
                    await _taskRepository.DeleteItemAsync(_task);

                _sourceProject?.Tasks.RemoveAll(t => ReferenceEquals(t, _task) || (_task.ID > 0 && t.ID == _task.ID));
                await Shell.Current.GoToAsync("..?refresh=true");
                await AppShell.DisplayToastAsync("Tarea eliminada");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo eliminar la tarea. Inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
