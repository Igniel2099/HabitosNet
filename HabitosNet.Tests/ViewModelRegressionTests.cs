using HabitosNet;
using HabitosNet.Models;
using HabitosNet.PageModels;

internal static class ViewModelRegressionTests
{
    public static (string Name, Func<TestDatabase, Task> Run)[] Cases =>
    [
        ("Completar una tarea de un proyecto nuevo no crea huérfanas", CompleteDraftTask),
        ("Guardar sin proyecto conserva título y pantalla", RejectTaskWithoutProject),
        ("Un fallo SQL no muestra éxito ni abandona la tarea", FailedTaskSaveStaysOpen),
        ("La tarea está persistida antes de navegar", SuccessfulTaskSaveBeforeNavigation),
        ("Un error al actualizar conserva la tarea original", FailedTaskUpdatePreservesOriginal),
        ("La categoría usada permanece visible con explicación", UsedCategoryStaysVisible),
        ("Cancelar el restablecimiento conserva los datos", CancelResetPreservesData),
        ("Se pueden editar y eliminar tareas del borrador", EditAndDeleteDraftTask),
    ];

    private static ProjectDetailPageModel ProjectModel(TestDatabase db) => new(db.Projects, db.Tasks, db.Categories, db.Tags, new());
    private static TaskDetailPageModel TaskModel(TestDatabase db) => new(db.Projects, db.Tasks, new());

    private static async Task Load(IQueryAttributable model, IDictionary<string, object> query)
    {
        model.ApplyQueryAttributes(query);
        // MAUI dispatches query attributes synchronously, while the real viewmodels
        // expose IsBusy until their asynchronous load finishes.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (model is ProjectDetailPageModel { IsBusy: true } or TaskDetailPageModel { IsBusy: true })
        {
            Check(DateTime.UtcNow < deadline, "La carga no terminó a tiempo.");
            await Task.Delay(1);
        }
    }

    private static async Task CompleteDraftTask(TestDatabase db)
    {
        var projectModel = ProjectModel(db);
        await Load(projectModel, new Dictionary<string, object>());
        projectModel.Name = "Proyecto nuevo";
        await projectModel.AddTaskCommand.ExecuteAsync(null);
        var taskParameters = Shell.Current.Navigations.Single().Parameters;
        var taskModel = TaskModel(db);
        await Load(taskModel, taskParameters);
        taskModel.Title = "Antes de guardar";
        await taskModel.SaveCommand.ExecuteAsync(null);
        await Load(projectModel, new Dictionary<string, object> { ["refresh"] = true });

        var draft = projectModel.Tasks.Single();
        draft.IsCompleted = true;
        await projectModel.TaskCompletedCommand.ExecuteAsync(draft);
        Check(draft.ID == 0 && draft.ProjectID == 0, "La tarea debe permanecer como borrador.");
        Check((await db.Tasks.ListAsync()).Count == 0, "No se debe insertar una tarea sin proyecto.");

        await projectModel.SaveCommand.ExecuteAsync(null);
        Check(!projectModel.HasValidationMessage, projectModel.ValidationMessage);
        var savedProject = (await db.Projects.ListAsync()).Single();
        var savedTask = savedProject.Tasks.Single();
        Check(savedTask.ID > 0 && savedTask.ProjectID == savedProject.ID && savedTask.IsCompleted,
            "El guardado debe conservar la relación y el estado completado.");
    }

    private static async Task RejectTaskWithoutProject(TestDatabase db)
    {
        await db.AddProject();
        var model = TaskModel(db);
        await Load(model, new Dictionary<string, object>());
        model.Title = "No perder este texto";
        model.IsCompleted = true;
        await model.SaveCommand.ExecuteAsync(null);
        Check(model.HasValidationMessage && model.ValidationMessage.Contains("proyecto"), "Debe pedir seleccionar un proyecto.");
        Check(model.Title == "No perder este texto" && model.IsCompleted, "Debe conservar todos los campos.");
        Check(Shell.Current.Navigations.Count == 0 && AppShell.Toasts.Count == 0, "No debe navegar ni anunciar éxito.");
        Check((await db.Tasks.ListAsync()).Count == 0, "No debe guardar sin proyecto.");
    }

    private static async Task FailedTaskSaveStaysOpen(TestDatabase db)
    {
        await db.AddProject();
        var model = TaskModel(db);
        await Load(model, new Dictionary<string, object>());
        model.Title = "Pendiente de guardar";
        model.SelectedProjectIndex = 0;
        await db.Execute("CREATE TRIGGER fail_task_insert BEFORE INSERT ON Task BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await model.SaveCommand.ExecuteAsync(null);
        Check(model.HasValidationMessage && !model.IsBusy, "Debe informar del fallo y permitir reintentar.");
        Check(model.Title == "Pendiente de guardar", "Debe conservar el texto escrito.");
        Check(Shell.Current.Navigations.Count == 0 && AppShell.Toasts.Count == 0, "Un fallo nunca debe anunciar éxito ni navegar.");
        Check((await db.Tasks.ListAsync()).Count == 0, "No debe haber guardado una tarea al fallar.");

        await db.Execute("DROP TRIGGER fail_task_insert;");
        await model.SaveCommand.ExecuteAsync(null);
        Check((await db.Tasks.ListAsync()).Count == 1, "El reintento debe guardar exactamente una tarea.");
        Check(Shell.Current.Navigations.Count == 1 && AppShell.Toasts.Count == 1, "Solo se confirma el reintento correcto.");
    }

    private static async Task SuccessfulTaskSaveBeforeNavigation(TestDatabase db)
    {
        var project = await db.AddProject();
        var model = TaskModel(db);
        await Load(model, new Dictionary<string, object>());
        model.Title = "Guardada antes de salir";
        model.SelectedProjectIndex = 0;
        var observedPersisted = false;
        Shell.Current.BeforeNavigation = async () =>
        {
            var saved = (await db.Tasks.ListAsync()).Single();
            observedPersisted = saved.Title == model.Title && saved.ProjectID == project.ID;
        };
        await model.SaveCommand.ExecuteAsync(null);
        Check(observedPersisted, "La navegación debe ocurrir después de persistir la tarea.");
        Check(!model.HasValidationMessage, model.ValidationMessage);
        Check(AppShell.Toasts.Single() == "Tarea guardada", "Debe confirmar el guardado correcto.");
    }

    private static async Task FailedTaskUpdatePreservesOriginal(TestDatabase db)
    {
        var project = await db.AddProject();
        var task = await db.AddTask(project);
        project.Tasks.Add(task);
        var originalTitle = task.Title;
        var model = TaskModel(db);
        await Load(model, new Dictionary<string, object> { ["id"] = task.ID, [TaskDetailPageModel.ProjectQueryKey] = project });
        model.Title = "Cambios pendientes";
        model.IsCompleted = true;
        await db.Execute("CREATE TRIGGER fail_task_update BEFORE UPDATE ON Task BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        await model.SaveCommand.ExecuteAsync(null);
        var persisted = (await db.Tasks.GetAsync(task.ID))!;
        Check(persisted.Title == originalTitle && !persisted.IsCompleted, "La versión guardada debe permanecer intacta.");
        Check(task.Title == originalTitle && !task.IsCompleted, "No debe alterar el objeto de la pantalla anterior.");
        Check(model.Title == "Cambios pendientes" && model.IsCompleted, "El editor debe conservar el intento de modificación.");
        Check(model.HasValidationMessage && Shell.Current.Navigations.Count == 0 && AppShell.Toasts.Count == 0, "Debe quedarse abierto con el error.");
    }

    private static async Task UsedCategoryStaysVisible(TestDatabase db)
    {
        var category = new Category { Title = "Personal" };
        await db.Categories.SaveItemAsync(category);
        await db.AddProject(categoryId: category.ID);
        var model = new ManageMetaPageModel(db.Categories, db.Tags, db.Seed);
        await model.AppearingCommand.ExecuteAsync(null);
        await model.DeleteCategoryCommand.ExecuteAsync(model.Categories.Single());
        Check(model.Categories.Single().ID == category.ID, "La categoría debe permanecer en pantalla.");
        Check(model.ValidationMessage.Contains("Reasigna"), "Debe explicar cómo eliminar una categoría utilizada.");
        Check(AppShell.Toasts.Count == 0, "No debe confirmar una eliminación rechazada.");
    }

    private static async Task CancelResetPreservesData(TestDatabase db)
    {
        var project = await db.AddProject("Conservar");
        var model = new ManageMetaPageModel(db.Categories, db.Tags, db.Seed);
        Shell.Current.ConfirmNextAlert = false;
        await model.ResetCommand.ExecuteAsync(null);
        Check((await db.Projects.GetAsync(project.ID))!.Name == "Conservar", "Cancelar debe preservar los datos.");
        Check(Shell.Current.Navigations.Count == 0 && AppShell.Toasts.Count == 0 && !model.IsBusy, "Cancelar debe dejar la pantalla disponible.");
    }

    private static async Task EditAndDeleteDraftTask(TestDatabase db)
    {
        var task = new ProjectTask { Title = "Inicial" };
        var project = new Project { Name = "Borrador", Tasks = [task] };
        var query = new Dictionary<string, object>
        {
            [TaskDetailPageModel.ProjectQueryKey] = project,
            [TaskDetailPageModel.TaskQueryKey] = task
        };
        var model = TaskModel(db);
        await Load(model, query);
        model.Title = "Editada";
        await model.SaveCommand.ExecuteAsync(null);
        Check(project.Tasks.Single().Title == "Editada" && task.ID == 0, "Editar un borrador debe actualizarlo sin duplicarlo.");
        await model.DeleteCommand.ExecuteAsync(null);
        Check(project.Tasks.Count == 0 && (await db.Tasks.ListAsync()).Count == 0, "Eliminar un borrador no debe crear registros.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
