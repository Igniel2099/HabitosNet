using HabitosNet.Data;
using HabitosNet.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

SQLitePCL.Batteries_V2.Init();

var tests = new (string Name, Func<TestDatabase, Task> Run)[]
{
    ("Borrar proyecto elimina sus dependencias y conserva las ajenas", DeleteProjectDependencies),
    ("Un fallo de borrado revierte proyecto, tareas y etiquetas", DeleteProjectRollback),
    ("El borrado funciona con tablas antiguas sin claves foráneas", DeleteLegacyProject),
    ("El borrado funciona sin tablas de tareas y etiquetas todavía", DeleteProjectWithoutChildTables),
    ("No se puede borrar una categoría usada", RejectUsedCategory),
    ("Se puede borrar una categoría libre y reasignada", DeleteUnusedCategory),
    ("Se puede borrar una categoría antes de crear proyectos", DeleteCategoryWithoutProjectTable),
    ("Una tarea exige un proyecto guardado y existente", RejectOrphanTasks),
    ("Una tarea completada conserva su proyecto y su estado", SaveCompletedTask),
    ("Un proyecto rechaza categorías inexistentes y permite ninguna", RejectMissingCategory),
    ("Restablecer con JSON inválido conserva los datos", RejectInvalidSeed),
    ("Restablecer espera al borrado y carga todas las relaciones", LoadValidSeed),
    ("Los datos de ejemplo de la aplicación conservan nombres y estados", LoadPackagedSeed),
}.Concat(ViewModelRegressionTests.Cases).ToArray();

var failures = 0;
foreach (var (name, run) in tests)
{
    using var database = new TestDatabase();
    try
    {
        await run(database);
        Console.WriteLine($"OK: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"ERROR: {name}\n{exception}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} pruebas correctas.");
return failures == 0 ? 0 : 1;

static async Task DeleteProjectDependencies(TestDatabase db)
{
    var first = await db.AddProject("Eliminar");
    var second = await db.AddProject("Conservar");
    var firstTask = await db.AddTask(first);
    var secondTask = await db.AddTask(second);
    var tag = new Tag { Title = "Compartida" };
    await db.Tags.SaveItemAsync(tag, first.ID);
    await db.Tags.SaveItemAsync(tag, second.ID);

    Equal(1, await db.Projects.DeleteItemAsync(first));
    Equal<Project?>(null, await db.Projects.GetAsync(first.ID));
    Equal<ProjectTask?>(null, await db.Tasks.GetAsync(firstTask.ID));
    Equal(0, (await db.Tags.ListAsync(first.ID)).Count);
    Equal(second.ID, (await db.Projects.GetAsync(second.ID))!.ID);
    Equal(secondTask.ID, (await db.Tasks.GetAsync(secondTask.ID))!.ID);
    Equal(tag.ID, (await db.Tags.ListAsync(second.ID)).Single().ID);
    Equal(1, (await db.Tags.ListAsync()).Count);
    Equal(0, await db.Projects.DeleteItemAsync(first));
}

static async Task DeleteProjectRollback(TestDatabase db)
{
    var project = await db.AddProject();
    var task = await db.AddTask(project);
    var tag = new Tag { Title = "Conservar" };
    await db.Tags.SaveItemAsync(tag, project.ID);
    await db.Execute("CREATE TRIGGER fail_project_delete BEFORE DELETE ON Project BEGIN SELECT RAISE(ABORT, 'test failure'); END;");

    await Throws<SqliteException>(() => db.Projects.DeleteItemAsync(project));
    Equal(project.ID, (await db.Projects.GetAsync(project.ID))!.ID);
    Equal(task.ID, (await db.Tasks.GetAsync(task.ID))!.ID);
    Equal(tag.ID, (await db.Tags.ListAsync(project.ID)).Single().ID);
}

static async Task DeleteLegacyProject(TestDatabase db)
{
    // Exact pre-existing schema, populated before any repository initializes it.
    await db.Execute("""
        CREATE TABLE Project (ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT NOT NULL, Icon TEXT NOT NULL, CategoryID INTEGER NOT NULL);
        CREATE TABLE Task (ID INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, IsCompleted INTEGER NOT NULL, ProjectID INTEGER NOT NULL);
        CREATE TABLE Tag (ID INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Color TEXT NOT NULL);
        CREATE TABLE ProjectsTags (ProjectID INTEGER NOT NULL, TagID INTEGER NOT NULL, PRIMARY KEY(ProjectID, TagID));
        INSERT INTO Project VALUES (42, 'Anterior', '', '', 0);
        INSERT INTO Task VALUES (7, 'Anterior', 1, 42);
        INSERT INTO Tag VALUES (9, 'Anterior', '#FF0000');
        INSERT INTO ProjectsTags VALUES (42, 9);
        """);
    Equal(1, await db.Projects.DeleteItemAsync(new Project { ID = 42 }));
    Equal(0, (await db.Tasks.ListAsync()).Count);
    Equal(0, (await db.Tags.ListAsync(42)).Count);
    Equal(1, (await db.Tags.ListAsync()).Count);
}

static async Task DeleteProjectWithoutChildTables(TestDatabase db)
{
    var project = await db.AddProject();
    Equal(1, await db.Projects.DeleteItemAsync(project));
    Equal(0, (await db.Projects.ListAsync()).Count);
}

static async Task RejectUsedCategory(TestDatabase db)
{
    var category = new Category { Title = "En uso" };
    await db.Categories.SaveItemAsync(category);
    var project = await db.AddProject(categoryId: category.ID);
    var exception = await Throws<InvalidOperationException>(() => db.Categories.DeleteItemAsync(category));
    True(exception.Message.Contains("está en uso"), "Debe explicar cómo resolver la categoría en uso.");
    Equal(category.ID, (await db.Categories.GetAsync(category.ID))!.ID);
    Equal(category.ID, (await db.Projects.GetAsync(project.ID))!.CategoryID);
}

static async Task DeleteUnusedCategory(TestDatabase db)
{
    var oldCategory = new Category { Title = "Anterior" };
    var newCategory = new Category { Title = "Nueva" };
    await db.Categories.SaveItemAsync(oldCategory);
    await db.Categories.SaveItemAsync(newCategory);
    var project = await db.AddProject(categoryId: oldCategory.ID);
    project.CategoryID = newCategory.ID;
    await db.Projects.SaveItemAsync(project);
    Equal(1, await db.Categories.DeleteItemAsync(oldCategory));
    Equal<Category?>(null, await db.Categories.GetAsync(oldCategory.ID));
    Equal(newCategory.ID, (await db.Projects.GetAsync(project.ID))!.CategoryID);
}

static async Task DeleteCategoryWithoutProjectTable(TestDatabase db)
{
    var category = new Category { Title = "Libre" };
    await db.Categories.SaveItemAsync(category);
    Equal(1, await db.Categories.DeleteItemAsync(category));
}

static async Task RejectOrphanTasks(TestDatabase db)
{
    var draft = new ProjectTask { Title = "Borrador", IsCompleted = true };
    await Throws<InvalidOperationException>(() => db.Tasks.SaveItemAsync(draft));
    Equal(0, draft.ID);
    draft.ProjectID = 999;
    await Throws<InvalidOperationException>(() => db.Tasks.SaveItemAsync(draft));
    Equal(0, (await db.Tasks.ListAsync()).Count);

    var project = await db.AddProject();
    var task = await db.AddTask(project);
    task.ProjectID = 999;
    await Throws<InvalidOperationException>(() => db.Tasks.SaveItemAsync(task));
    Equal(project.ID, (await db.Tasks.GetAsync(task.ID))!.ProjectID);
    await db.Projects.DeleteItemAsync(project);
    draft.ProjectID = project.ID;
    await Throws<InvalidOperationException>(() => db.Tasks.SaveItemAsync(draft));
    Equal(0, (await db.Tasks.ListAsync()).Count);
}

static async Task SaveCompletedTask(TestDatabase db)
{
    var project = await db.AddProject();
    var task = new ProjectTask { Title = "Completada", IsCompleted = true, ProjectID = project.ID };
    await db.Tasks.SaveItemAsync(task);
    var loaded = (await db.Tasks.GetAsync(task.ID))!;
    Equal(project.ID, loaded.ProjectID);
    True(loaded.IsCompleted, "Debe conservar el estado completado.");
}

static async Task RejectMissingCategory(TestDatabase db)
{
    var project = new Project { Name = "Sin categoría", CategoryID = 88 };
    await Throws<InvalidOperationException>(() => db.Projects.SaveItemAsync(project));
    Equal(0, project.ID);
    project.CategoryID = 0;
    await db.Projects.SaveItemAsync(project);
    var category = new Category { Title = "Borrada" };
    await db.Categories.SaveItemAsync(category);
    await db.Categories.DeleteItemAsync(category);
    project.CategoryID = category.ID;
    await Throws<InvalidOperationException>(() => db.Projects.SaveItemAsync(project));
    Equal(0, (await db.Projects.GetAsync(project.ID))!.CategoryID);
}

static async Task RejectInvalidSeed(TestDatabase db)
{
    var project = await db.AddProject("Mis datos");
    var task = await db.AddTask(project);
    foreach (var invalid in new[] { "{broken", "null", "{}", "{\"Projects\":[null]}", "{\"Projects\":[{\"Name\":\"Ejemplo\",\"Tasks\":null}]}" })
    {
        FileSystem.PackageFileContents = invalid;
        await Throws<InvalidOperationException>(() => db.Seed.LoadSeedDataAsync());
        Equal(project.Name, (await db.Projects.GetAsync(project.ID))!.Name);
        Equal(task.ID, (await db.Tasks.GetAsync(task.ID))!.ID);
    }
}

static async Task LoadValidSeed(TestDatabase db)
{
    await db.AddProject("Anterior");
    FileSystem.PackageFileContents = """
        {"Projects":[{"Name":"Ejemplo","Category":{"Title":"Personal","Color":"#123456"},"Tasks":[{"Title":"Completada","IsCompleted":true}],"Tags":[{"Title":"Diaria","Color":"#345678"}]}]}
        """;
    await db.Seed.LoadSeedDataAsync();
    var project = (await db.Projects.ListAsync()).Single();
    Equal("Ejemplo", project.Name);
    Equal(project.CategoryID, (await db.Categories.ListAsync()).Single().ID);
    Equal(project.ID, project.Tasks.Single().ProjectID);
    True(project.Tasks.Single().IsCompleted, "Debe cargar el estado de la tarea.");
    Equal("Diaria", project.Tags.Single().Title);
}

static async Task LoadPackagedSeed(TestDatabase db)
{
    FileSystem.PackageFileContents = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "SeedData.json"));
    using var document = System.Text.Json.JsonDocument.Parse(FileSystem.PackageFileContents);
    var expectedProjects = document.RootElement.GetProperty("Projects").EnumerateArray().ToArray();
    await db.Seed.LoadSeedDataAsync();
    var loadedProjects = await db.Projects.ListAsync();
    Equal(expectedProjects.Length, loadedProjects.Count);
    foreach (var expected in expectedProjects)
    {
        var project = loadedProjects.Single(p => p.Name == expected.GetProperty("Name").GetString());
        var expectedTasks = expected.GetProperty("Tasks").EnumerateArray().ToArray();
        Equal(expectedTasks.Length, project.Tasks.Count);
        foreach (var expectedTask in expectedTasks)
        {
            var task = project.Tasks.Single(t => t.Title == expectedTask.GetProperty("Title").GetString());
            Equal(expectedTask.GetProperty("IsCompleted").GetBoolean(), task.IsCompleted);
            Equal(project.ID, task.ProjectID);
        }
    }
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Esperado: {expected}; obtenido: {actual}");
}

static void True(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task<T> Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T exception) { return exception; }
    throw new InvalidOperationException($"Se esperaba {typeof(T).Name}.");
}

internal sealed class TestDatabase : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "HabitosNet.Tests", Guid.NewGuid().ToString("N"));
    public TaskRepository Tasks { get; } = new(NullLogger<TaskRepository>.Instance);
    public TagRepository Tags { get; } = new(NullLogger<TagRepository>.Instance);
    public CategoryRepository Categories { get; } = new(NullLogger<CategoryRepository>.Instance);
    public ProjectRepository Projects { get; }
    public SeedDataService Seed { get; }

    public TestDatabase()
    {
        Directory.CreateDirectory(_directory);
        FileSystem.AppDataDirectory = _directory;
        Shell.Current = new();
        HabitosNet.AppShell.Toasts.Clear();
        Preferences.Default = new();
        Projects = new(Tasks, Tags, NullLogger<ProjectRepository>.Instance);
        Seed = new(Projects, Tasks, Tags, Categories, NullLogger<SeedDataService>.Instance);
    }

    public async Task<Project> AddProject(string name = "Proyecto", int categoryId = 0)
    {
        var project = new Project { Name = name, CategoryID = categoryId };
        await Projects.SaveItemAsync(project);
        return project;
    }

    public async Task<ProjectTask> AddTask(Project project)
    {
        var task = new ProjectTask { Title = "Tarea", ProjectID = project.ID };
        await Tasks.SaveItemAsync(task);
        return task;
    }

    public async Task Execute(string sql)
    {
        await using var connection = new SqliteConnection(Constants.DatabasePath);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }
}
