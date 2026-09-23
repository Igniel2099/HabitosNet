using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HabitosNet.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace HabitosNet.PageModels
{
    public partial class ManageMetaPageModel : ObservableObject
    {
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;
        private readonly SeedDataService _seedDataService;
        private bool _isLoaded;

        [ObservableProperty]
        private ObservableCollection<Category> _categories = [];

        [ObservableProperty]
        private ObservableCollection<Tag> _tags = [];

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
        private string _validationMessage = string.Empty;

        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
        public bool CanSave => !IsBusy && _isLoaded;

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(CanSave));
            SaveCategoriesCommand.NotifyCanExecuteChanged();
            SaveTagsCommand.NotifyCanExecuteChanged();
            AddCategoryCommand.NotifyCanExecuteChanged();
            AddTagCommand.NotifyCanExecuteChanged();
            DeleteCategoryCommand.NotifyCanExecuteChanged();
            DeleteTagCommand.NotifyCanExecuteChanged();
            ResetCommand.NotifyCanExecuteChanged();
        }

        public ManageMetaPageModel(CategoryRepository categoryRepository, TagRepository tagRepository, SeedDataService seedDataService)
        {
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _seedDataService = seedDataService;
        }

        private async Task LoadData()
        {
            _isLoaded = false;
            IsBusy = true;
            ValidationMessage = string.Empty;
            try
            {
                var categories = await _categoryRepository.ListAsync();
                var tags = await _tagRepository.ListAsync();
                Categories = new(categories);
                Tags = new(tags);
                _isLoaded = true;
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudieron cargar las categorías y etiquetas. Inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task Appearing() => LoadData();

        private bool ValidateItems(IEnumerable<(string Title, string Color)> items, string itemName)
        {
            foreach (var (title, color) in items)
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    ValidationMessage = $"Escribe un nombre para cada {itemName} antes de guardar.";
                    return false;
                }

                if (!Regex.IsMatch(color?.Trim() ?? string.Empty, "^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$"))
                {
                    ValidationMessage = $"El color de «{title.Trim()}» debe tener el formato #RRGGBB o #AARRGGBB, por ejemplo #2563EB.";
                    return false;
                }
            }

            return true;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveCategories()
        {
            ValidationMessage = string.Empty;
            if (!ValidateItems(Categories.Select(c => (c.Title, c.Color)), "categoría"))
                return;

            IsBusy = true;
            try
            {
                foreach (var category in Categories)
                {
                    category.Title = category.Title.Trim();
                    category.Color = category.Color.Trim();
                    await _categoryRepository.SaveItemAsync(category);
                }

                await AppShell.DisplayToastAsync("Categorías guardadas");
                SemanticScreenReader.Announce("Categorías guardadas");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudieron guardar todas las categorías. Revisa los datos e inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task DeleteCategory(Category category)
        {
            IsBusy = true;
            ValidationMessage = string.Empty;
            try
            {
                if (category.ID > 0)
                    await _categoryRepository.DeleteItemAsync(category);
                Categories.Remove(category);
                await AppShell.DisplayToastAsync("Categoría eliminada");
                SemanticScreenReader.Announce("Categoría eliminada");
            }
            catch (InvalidOperationException exception)
            {
                ValidationMessage = exception.Message;
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo eliminar la categoría. Inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private Task AddCategory()
        {
            ValidationMessage = string.Empty;
            Categories.Add(new Category { Color = "#2563EB" });
            SemanticScreenReader.Announce("Nueva categoría. Escribe su nombre y guarda los cambios.");
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveTags()
        {
            ValidationMessage = string.Empty;
            if (!ValidateItems(Tags.Select(t => (t.Title, t.Color)), "etiqueta"))
                return;

            IsBusy = true;
            try
            {
                foreach (var tag in Tags)
                {
                    tag.Title = tag.Title.Trim();
                    tag.Color = tag.Color.Trim();
                    await _tagRepository.SaveItemAsync(tag);
                }

                await AppShell.DisplayToastAsync("Etiquetas guardadas");
                SemanticScreenReader.Announce("Etiquetas guardadas");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudieron guardar todas las etiquetas. Revisa los datos e inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task DeleteTag(Tag tag)
        {
            IsBusy = true;
            ValidationMessage = string.Empty;
            try
            {
                if (tag.ID > 0)
                    await _tagRepository.DeleteItemAsync(tag);
                Tags.Remove(tag);
                await AppShell.DisplayToastAsync("Etiqueta eliminada");
                SemanticScreenReader.Announce("Etiqueta eliminada");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudo eliminar la etiqueta. Inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private Task AddTag()
        {
            ValidationMessage = string.Empty;
            Tags.Add(new Tag { Color = "#2563EB" });
            SemanticScreenReader.Announce("Nueva etiqueta. Escribe su nombre y guarda los cambios.");
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Reset()
        {
            IsBusy = true;
            ValidationMessage = string.Empty;
            try
            {
                if (!await Shell.Current.DisplayAlertAsync("Restaurar datos de ejemplo", "Se sustituirán todos tus proyectos, tareas, categorías y etiquetas por los datos de ejemplo. Esta acción no se puede deshacer.", "Restaurar", "Cancelar"))
                    return;

                await _seedDataService.LoadSeedDataAsync();
                Preferences.Default.Set("is_seeded", true);
                await Shell.Current.GoToAsync("//main");
                await AppShell.DisplayToastAsync("Datos de ejemplo restaurados");
            }
            catch (Exception)
            {
                ValidationMessage = "No se pudieron restaurar los datos de ejemplo. Inténtalo de nuevo.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
