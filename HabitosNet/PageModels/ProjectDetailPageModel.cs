using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Models;

namespace HabitosNet.PageModels
{
    public partial class ProjectDetailPageModel : ObservableObject, IQueryAttributable, IProjectTaskPageModel
    {
        private Project? _project;
        private readonly ProjectRepository _projectRepository;
        private readonly TaskRepository _taskRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;
        private readonly ModalErrorHandler _errorHandler;
        private bool _canDelete;
        private bool _isLoaded;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCompletedTasks))]
        private List<ProjectTask> _tasks = [];

        [ObservableProperty]
        private List<Category> _categories = [];

        [ObservableProperty]
        private Category? _category;

        [ObservableProperty]
        private int _categoryIndex = -1;

        [ObservableProperty]
        private List<Tag> _allTags = [];

        [ObservableProperty]
        private IList<object> _selectedTags = new List<object>();

        [ObservableProperty]
        private IconData _icon;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private List<IconData> _icons =
        [
            new() { Icon = FluentUI.ribbon_24_regular, Description = "Cinta" },
            new() { Icon = FluentUI.ribbon_star_24_regular, Description = "Estrella" },
            new() { Icon = FluentUI.trophy_24_regular, Description = "Trofeo" },
            new() { Icon = FluentUI.badge_24_regular, Description = "Insignia" },
            new() { Icon = FluentUI.book_24_regular, Description = "Libro" },
            new() { Icon = FluentUI.people_24_regular, Description = "Personas" },
            new() { Icon = FluentUI.bot_24_regular, Description = "Robot" }
        ];

        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
        public bool CanSave => !IsBusy && _isLoaded;
        public bool HasCompletedTasks => Tasks.Any(t => t.IsCompleted);

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
            AddTaskCommand.NotifyCanExecuteChanged();
            CleanTasksCommand.NotifyCanExecuteChanged();
            TaskCompletedCommand.NotifyCanExecuteChanged();
            NavigateToTaskCommand.NotifyCanExecuteChanged();
            ToggleTagCommand.NotifyCanExecuteChanged();
        }

        public ProjectDetailPageModel(ProjectRepository projectRepository, TaskRepository taskRepository, CategoryRepository categoryRepository, TagRepository tagRepository, ModalErrorHandler errorHandler)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _errorHandler = errorHandler;
            _icon = _icons.First();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Returning from a task must preserve unsaved project fields and tag choices.
            if (query.ContainsKey("refresh") && _project is not null)
                RefreshData().FireAndForgetSafeAsync(_errorHandler);
            else if (query.TryGetValue("id", out var id))
                LoadData(Convert.ToInt32(id)).FireAndForgetSafeAsync(_errorHandler);
            else
                LoadData(null).FireAndForgetSafeAsync(_errorHandler);
        }

        private void SetTasks(IEnumerable<ProjectTask> tasks)
        {
            Tasks = new(tasks);
            if (_project is not null)
                _project.Tasks = Tasks;
        }

        private async Task RefreshData()
        {
            IsBusy = true;
            try
            {
                if (_project is not null)
                {
                    if (_project.ID == 0)
                        SetTasks(_project.Tasks);
                    else
                    {
                        // Retain pending tasks if an earlier project save completed only partially.
                        var pendingTasks = _project.Tasks.Where(t => t.ID == 0).ToList();
                        var savedTasks = await _taskRepository.ListAsync(_project.ID);
                        SetTasks(savedTasks.Concat(pendingTasks));
                    }
                }
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudieron actualizar las tareas. Inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadData(int? id)
        {
            _isLoaded = false;
            IsBusy = true;
            ValidationMessage = string.Empty;
            CanDelete = false;
            Name = string.Empty;
            Description = string.Empty;
            Category = null;
            CategoryIndex = -1;
            Categories = [];
            AllTags = [];
            SelectedTags = new List<object>();
            Icon = Icons.First();
            Tasks = [];
            _project = null;

            try
            {
                _project = id.HasValue ? await _projectRepository.GetAsync(id.Value) : new Project();
                if (_project is null)
                {
                    ValidationMessage = "No se encontró el proyecto. Vuelve al listado y actualízalo.";
                    return;
                }

                Name = _project.Name;
                Description = _project.Description;
                SetTasks(_project.Tasks);
                Icon = Icons.FirstOrDefault(icon => icon.Icon == _project.Icon) ?? Icons.First();
                Categories = await _categoryRepository.ListAsync();
                CategoryIndex = Categories.FindIndex(c => c.ID == _project.CategoryID);
                Category = CategoryIndex >= 0 ? Categories[CategoryIndex] : null;

                var tags = await _tagRepository.ListAsync();
                foreach (var tag in tags)
                    tag.IsSelected = _project.Tags.Any(t => t.ID == tag.ID);
                AllTags = tags;
                SelectedTags = tags.Where(t => t.IsSelected).Cast<object>().ToList();
                _isLoaded = true;
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo cargar el proyecto. Vuelve atrás e inténtalo de nuevo.";
            }
            finally
            {
                CanDelete = _project?.ID > 0;
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task TaskCompleted(ProjectTask task)
        {
            ValidationMessage = string.Empty;
            // Draft tasks belong only to memory until their parent project has an ID.
            if (_project is null || _project.ID == 0 || task.ID == 0)
            {
                OnPropertyChanged(nameof(HasCompletedTasks));
                return;
            }

            IsBusy = true;
            try
            {
                await _taskRepository.SaveItemAsync(task);
            }
            catch (Exception)
            {
                task.IsCompleted = !task.IsCompleted;
                ValidationMessage = "No se pudo cambiar el estado de la tarea. Inténtalo de nuevo.";
            }
            finally
            {
                OnPropertyChanged(nameof(HasCompletedTasks));
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            ValidationMessage = string.Empty;
            if (_project is null || !_isLoaded)
            {
                ValidationMessage = "No se pudo cargar el proyecto. Vuelve atrás e inténtalo de nuevo.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationMessage = "Escribe un nombre para el proyecto.";
                return;
            }

            IsBusy = true;
            try
            {
                _project.Name = Name.Trim();
                _project.Description = Description.Trim();
                _project.CategoryID = Category?.ID ?? 0;
                _project.Icon = Icon?.Icon ?? FluentUI.ribbon_24_regular;
                await _projectRepository.SaveItemAsync(_project);

                foreach (var tag in AllTags)
                {
                    if (tag.IsSelected)
                        await _tagRepository.SaveItemAsync(tag, _project.ID);
                    else
                        await _tagRepository.DeleteItemAsync(tag, _project.ID);
                }
                _project.Tags = AllTags.Where(t => t.IsSelected).ToList();

                foreach (var task in _project.Tasks)
                {
                    if (task.ID == 0)
                    {
                        task.ProjectID = _project.ID;
                        await _taskRepository.SaveItemAsync(task);
                    }
                }

                await Shell.Current.GoToAsync("..");
                await AppShell.DisplayToastAsync("Proyecto guardado");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo completar el guardado. Tus cambios siguen aquí; inténtalo de nuevo.";
            }
            finally
            {
                CanDelete = _project.ID > 0;
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task AddTask()
        {
            if (_project is null)
                return;

            _project.Name = Name.Trim();
            await Shell.Current.GoToAsync("task", new ShellNavigationQueryParameters
            {
                { TaskDetailPageModel.ProjectQueryKey, _project }
            });
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task Delete()
        {
            if (_project is null || _project.ID == 0)
                return;

            IsBusy = true;
            ValidationMessage = string.Empty;
            try
            {
                if (!await Shell.Current.DisplayAlertAsync("Eliminar proyecto", "Se eliminarán el proyecto y todas sus tareas. Esta acción no se puede deshacer.", "Eliminar", "Cancelar"))
                    return;

                await _projectRepository.DeleteItemAsync(_project);
                await Shell.Current.GoToAsync("..");
                await AppShell.DisplayToastAsync("Proyecto eliminado");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo eliminar el proyecto. Inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private Task NavigateToTask(ProjectTask task)
        {
            if (_project is null)
                return Task.CompletedTask;

            var parameters = new ShellNavigationQueryParameters
            {
                { TaskDetailPageModel.ProjectQueryKey, _project }
            };
            if (task.ID == 0)
                parameters.Add(TaskDetailPageModel.TaskQueryKey, task);
            else
                parameters.Add("id", task.ID);
            return Shell.Current.GoToAsync("task", parameters);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        internal Task ToggleTag(Tag tag)
        {
            tag.IsSelected = !tag.IsSelected;
            AllTags = new(AllTags);
            SelectedTags = AllTags.Where(t => t.IsSelected).Cast<object>().ToList();
            SemanticScreenReader.Announce($"{tag.Title}: {(tag.IsSelected ? "seleccionada" : "sin seleccionar")}");
            return Task.CompletedTask;
        }

        [RelayCommand]
        private void IconSelected(IconData icon)
            => SemanticScreenReader.Announce($"{icon.Description}: seleccionado");

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task CleanTasks()
        {
            IsBusy = true;
            ValidationMessage = string.Empty;
            try
            {
                var completedTasks = Tasks.Where(t => t.IsCompleted).ToArray();
                foreach (var task in completedTasks)
                {
                    if (task.ID > 0)
                        await _taskRepository.DeleteItemAsync(task);
                    Tasks.Remove(task);
                }

                await AppShell.DisplayToastAsync("Tareas completadas eliminadas");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudieron eliminar todas las tareas completadas. Inténtalo de nuevo.";
            }
            finally
            {
                SetTasks(Tasks);
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task SelectionChanged(object parameter)
        {
            if (parameter is not IEnumerable<object> selection)
                return Task.CompletedTask;

            var selectedIds = selection.OfType<Tag>().Select(t => t.ID).ToHashSet();
            foreach (var tag in AllTags)
                tag.IsSelected = selectedIds.Contains(tag.ID);
            AllTags = new(AllTags);
            return Task.CompletedTask;
        }
    }
}
