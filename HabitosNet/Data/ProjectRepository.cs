using HabitosNet.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace HabitosNet.Data
{
    /// <summary>
    /// Repository class for managing projects in the database.
    /// </summary>
    public class ProjectRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;
        private readonly TaskRepository _taskRepository;
        private readonly TagRepository _tagRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectRepository"/> class.
        /// </summary>
        /// <param name="taskRepository">The task repository instance.</param>
        /// <param name="tagRepository">The tag repository instance.</param>
        /// <param name="logger">The logger instance.</param>
        public ProjectRepository(TaskRepository taskRepository, TagRepository tagRepository, ILogger<ProjectRepository> logger)
        {
            _taskRepository = taskRepository;
            _tagRepository = tagRepository;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the database connection and creates the Project table if it does not exist.
        /// </summary>
        private async Task Init()
        {
            if (_hasBeenInitialized)
                return;

            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            try
            {
                var createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Project (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                Icon TEXT NOT NULL,
                CategoryID INTEGER NOT NULL
            );";
                await createTableCmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Project table");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Retrieves a list of all projects from the database.
        /// </summary>
        /// <returns>A list of <see cref="Project"/> objects.</returns>
        public async Task<List<Project>> ListAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Project";
            var projects = new List<Project>();

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                projects.Add(new Project
                {
                    ID = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    Icon = reader.GetString(3),
                    CategoryID = reader.GetInt32(4)
                });
            }

            foreach (var project in projects)
            {
                project.Tags = await _tagRepository.ListAsync(project.ID);
                project.Tasks = await _taskRepository.ListAsync(project.ID);
            }

            return projects;
        }

        /// <summary>
        /// Retrieves a specific project by its ID.
        /// </summary>
        /// <param name="id">The ID of the project.</param>
        /// <returns>A <see cref="Project"/> object if found; otherwise, null.</returns>
        public async Task<Project?> GetAsync(int id)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Project WHERE ID = @id";
            selectCmd.Parameters.AddWithValue("@id", id);

            Project? project = null;
            await using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    project = new Project
                    {
                        ID = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        Icon = reader.GetString(3),
                        CategoryID = reader.GetInt32(4)
                    };
                }
            }

            // Close the reader before another repository initializes its tables.
            if (project is not null)
            {
                project.Tags = await _tagRepository.ListAsync(project.ID);
                project.Tasks = await _taskRepository.ListAsync(project.ID);
            }

            return project;
        }

        /// <summary>
        /// Saves a project to the database. If the project ID is 0, a new project is created; otherwise, the existing project is updated.
        /// </summary>
        /// <param name="item">The project to save.</param>
        /// <returns>The ID of the saved project.</returns>
        public async Task<int> SaveItemAsync(Project item)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction();
            if (item.CategoryID != 0)
            {
                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "SELECT EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Category')";
                var categoryExists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync()) != 0;
                if (categoryExists)
                {
                    checkCmd.CommandText = "SELECT EXISTS (SELECT 1 FROM Category WHERE ID = @categoryId)";
                    checkCmd.Parameters.AddWithValue("@categoryId", item.CategoryID);
                    categoryExists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync()) != 0;
                }

                if (!categoryExists)
                    throw new InvalidOperationException("La categoría seleccionada ya no existe. Selecciona otra categoría.");
            }

            using var saveCmd = connection.CreateCommand();
            saveCmd.Transaction = transaction;
            if (item.ID == 0)
            {
                saveCmd.CommandText = @"
                INSERT INTO Project (Name, Description, Icon, CategoryID)
                VALUES (@Name, @Description, @Icon, @CategoryID);
                SELECT last_insert_rowid();";
            }
            else
            {
                saveCmd.CommandText = @"
                UPDATE Project
                SET Name = @Name, Description = @Description, Icon = @Icon, CategoryID = @CategoryID
                WHERE ID = @ID";
                saveCmd.Parameters.AddWithValue("@ID", item.ID);
            }

            saveCmd.Parameters.AddWithValue("@Name", item.Name);
            saveCmd.Parameters.AddWithValue("@Description", item.Description);
            saveCmd.Parameters.AddWithValue("@Icon", item.Icon);
            saveCmd.Parameters.AddWithValue("@CategoryID", item.CategoryID);

            var result = await saveCmd.ExecuteScalarAsync();
            await transaction.CommitAsync();
            if (item.ID == 0)
            {
                item.ID = Convert.ToInt32(result);
            }

            return item.ID;
        }

        /// <summary>
        /// Deletes a project, its tasks, and its tag associations in one transaction.
        /// </summary>
        /// <param name="item">The project to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteItemAsync(Project item)
        {
            await Init();
            await _taskRepository.EnsureInitializedAsync();
            await _tagRepository.EnsureInitializedAsync();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            // Explicit cascading also protects databases created before foreign keys existed.
            await using var transaction = connection.BeginTransaction();
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.Parameters.AddWithValue("@ID", item.ID);

            deleteCmd.CommandText = "DELETE FROM Task WHERE ProjectID = @ID";
            await deleteCmd.ExecuteNonQueryAsync();
            deleteCmd.CommandText = "DELETE FROM ProjectsTags WHERE ProjectID = @ID";
            await deleteCmd.ExecuteNonQueryAsync();
            deleteCmd.CommandText = "DELETE FROM Project WHERE ID = @ID";
            var affectedProjects = await deleteCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return affectedProjects;
        }

        /// <summary>
        /// Drops the Project table from the database.
        /// </summary>
        public async Task DropTableAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var dropCmd = connection.CreateCommand();
            dropCmd.CommandText = "DROP TABLE IF EXISTS Project";
            await dropCmd.ExecuteNonQueryAsync();

            await _taskRepository.DropTableAsync();
            await _tagRepository.DropTableAsync();
            _hasBeenInitialized = false;
        }
    }
}
