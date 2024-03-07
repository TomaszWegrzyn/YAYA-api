namespace YAYA_api;

public static class TaskApiExtensions
{
    public static WebApplication AddTaskEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/CreateTask/",
                async (CreateTaskCommand command, IEventStore<Task, TaskId> eventStore) =>
                {
                    // We need to provide this GUID when creating, because it must be the same when re-applying the events
                    await eventStore.Add(Task.Create(new TaskId(Guid.NewGuid()), DateTime.Now, command.TaskPriority, command.TaskStatusId, command.Name));
                })
            .WithName("CreateTask")
            .WithOpenApi();

        app.MapGet(
                "/Tasks/{id}",
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

    public static WebApplication AddTaskStatusEndpoints(this WebApplication app)
    {
        // this should be a saga
        app.MapPost(
                "/CreateTaskStatus/",
                async (CreateTaskStatusCommand command, IEventStore<TaskStatus, TaskStatusId> taskStatusEventStore, IEventStore<TaskStatusNameLock, string> nameLockEventStore, CancellationToken cancellationToken) =>
                {
                    var nameLock = await nameLockEventStore.Find(command.Name, cancellationToken);
                    if (nameLock == null)
                    {
                        nameLock = TaskStatusNameLock.Create(command.Name);
                        await nameLockEventStore.Add(nameLock, cancellationToken);
                    }

                    if (nameLock.Locked)
                    {
                        throw new InvalidOperationException("Task status name is already in use!");
                    }

                    nameLock.StartAcquire(DateTime.Now);
                    var taskStatusId = new TaskStatusId(Guid.NewGuid());
                    var taskStatus = TaskStatus.Create(taskStatusId, DateTime.Now, command.Name);
                    await taskStatusEventStore.Add(taskStatus, cancellationToken);
                    try
                    {
                        nameLock.FinishAcquire(DateTime.Now);
                        await nameLockEventStore.Update(nameLock, ct: cancellationToken);

                    }
                    catch (Exception)
                    {
                        // if we fail to acquire the lock, we should remove the task status
                        await taskStatusEventStore.Delete(taskStatus, ct: cancellationToken);
                        throw;
                    }
                })
            .WithName("CreateTaskStatus")
            .WithOpenApi();

        app.MapGet(
                "/TaskStatus/{id}",
                async (Guid id, IEventStore<TaskStatus, TaskStatusId> eventStore, CancellationToken cancellationToken) =>
                {
                    var result = await eventStore.Find(new TaskStatusId(id), cancellationToken);
                    return result;
                })
            .WithName("GetTaskStatus")
            .WithOpenApi();
        return app;
    }
}


public class CreateTaskCommand
{
    public TaskPriority TaskPriority { get; }
    public string Name { get; }

    public TaskStatusId TaskStatusId { get; }

    public CreateTaskCommand(TaskPriority taskPriority, string name, TaskStatusId taskStatusId)
    {
        TaskPriority = taskPriority;
        Name = name;
        TaskStatusId = taskStatusId;
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

public class CreateTaskStatusCommand
{
    public string Name { get; }

    public CreateTaskStatusCommand(string name)
    {
        Name = name;
    }


}