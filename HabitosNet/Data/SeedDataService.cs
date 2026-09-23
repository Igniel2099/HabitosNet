using HabitosNet.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HabitosNet.Data
{
    public class SeedDataService
    {
        private readonly ProjectRepository _projectRepository;
        private readonly TaskRepository _taskRepository;
        private readonly TagRepository _tagRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly string _seedDataFilePath = "SeedData.json";
        private readonly ILogger<SeedDataService> _logger;

        public SeedDataService(ProjectRepository projectRepository, TaskRepository taskRepository, TagRepository tagRepository, CategoryRepository categoryRepository, ILogger<SeedDataService> logger)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
            _tagRepository = tagRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task LoadSeedDataAsync()
        {
            await using Stream templateStream = await FileSystem.OpenAppPackageFileAsync(_seedDataFilePath);

            ProjectsJson? payload;
            try
            {
                payload = JsonSerializer.Deserialize(templateStream, JsonContext.Default.ProjectsJson);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deserializing seed data");
                throw new InvalidOperationException("No se han podido leer los datos de ejemplo. Tus datos se han conservado.", e);
            }

            if (payload?.Projects is not { Count: > 0 } || payload.Projects.Any(project =>
                project is null || string.IsNullOrWhiteSpace(project.Name) || project.Description is null || project.Icon is null ||
                project.Tasks is null || project.Tasks.Any(task => task is null || string.IsNullOrWhiteSpace(task.Title)) ||
                project.Tags is null || project.Tags.Any(tag => tag is null || string.IsNullOrWhiteSpace(tag.Title) || tag.Color is null) ||
                (project.Category is not null && (string.IsNullOrWhiteSpace(project.Category.Title) || project.Category.Color is null))))
            {
                throw new InvalidOperationException("Los datos de ejemplo no son válidos. Tus datos se han conservado.");
            }

            await ClearTablesAsync();

            try
            {
                if (payload is not null)
                {
                    foreach (var project in payload.Projects)
                    {
                        if (project is null)
                        {
                            continue;
                        }

                        if (project.Category is not null)
                        {
                            await _categoryRepository.SaveItemAsync(project.Category);
                            project.CategoryID = project.Category.ID;
                        }

                        await _projectRepository.SaveItemAsync(project);

                        if (project?.Tasks is not null)
                        {
                            foreach (var task in project.Tasks)
                            {
                                task.ProjectID = project.ID;
                                await _taskRepository.SaveItemAsync(task);
                            }
                        }

                        if (project?.Tags is not null)
                        {
                            foreach (var tag in project.Tags)
                            {
                                await _tagRepository.SaveItemAsync(tag, project.ID);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error saving seed data");
                throw;
            }
        }

        private async Task ClearTablesAsync()
        {
            // ProjectRepository also clears tasks and tags; wait before recreating any table.
            await _projectRepository.DropTableAsync();
            await _categoryRepository.DropTableAsync();
        }
    }
}
