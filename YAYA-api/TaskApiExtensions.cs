namespace YAYA_api;

public static class TaskApiExtensions
{
    public static WebApplication AddTaskEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/CreateTask/",
                async (CreateTaskCommand command, IEventStore<Task, TaskId> eventStore) =>
                {
                    await eventStore.Add(Task.Create(new TaskId(Guid.NewGuid()), DateTime.Now, command.TaskPriority, command.Name));
                })
            .WithName("CreateTask")
            .WithOpenApi();

        app.MapGet(
                "/GetTasks/{id}",
                async (Guid id, IEventStore<Task, TaskId> eventStore, CancellationToken cancellationToken) =>
                {
                    var result = await eventStore.Find(new TaskId(id), cancellationToken);
                    return result;
                })
            .WithName("GetTask")
            .WithOpenApi();

        app.MapPost(
                "/IncreasePriority/",
                async (IncreaseTaskPriorityCommand command, IEventStore<Task, TaskId> eventStore, CancellationToken cancellationToken) =>
                {
                    var task = await eventStore.Find(new TaskId(command.TaskId), cancellationToken);
                    task.IncreasePriority();
                    await eventStore.Update(task, ct: cancellationToken);
                })
            .WithName("IncreasePriority")
            .WithOpenApi();

        app.MapPost(
                "/DecreasePriority/",
                async (DecreaseTaskPriorityCommand command, IEventStore<Task, TaskId> eventStore, CancellationToken cancellationToken) =>
                {
                    var task = await eventStore.Find(new TaskId(command.TaskId), cancellationToken);
                    task.DecreasePriority();
                    await eventStore.Update(task, ct: cancellationToken);
                })
            .WithName("DecreasePriority")
            .WithOpenApi();
        return app;

    }
}


public class CreateTaskCommand
{
    public TaskPriority TaskPriority { get; }
    public string Name { get; }

    public CreateTaskCommand(TaskPriority taskPriority, string name)
    {
        TaskPriority = taskPriority;
        Name = name;
    }
}

public class IncreaseTaskPriorityCommand
{
    public Guid TaskId { get; }

    public IncreaseTaskPriorityCommand(Guid taskId)
    {
        TaskId = taskId;
    }
}

public class DecreaseTaskPriorityCommand
{
    public Guid TaskId { get; }

    public DecreaseTaskPriorityCommand(Guid taskId)
    {
        TaskId = taskId;
    }
}