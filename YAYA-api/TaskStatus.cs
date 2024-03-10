using Microsoft.Extensions.DependencyInjection;

namespace YAYA_api;

public class TaskStatusId : StronglyTypedValue<Guid>
{

    public TaskStatusId(Guid value) : base(value)
    {
    }

    public static readonly TaskStatusId Default = new TaskStatusId(new Guid("8f95336d-c662-41f0-8007-36debeb0ba75"));

}

public class TaskStatus : Aggregate<TaskStatusId>
{
    public TaskStatus(TaskStatusId id, DateTime createdAt, string name) : base(id)
    {
    }

    public TaskStatus(TaskStatusCreatedEvent creationEvent) :
        this(creationEvent.TaskStatusId, creationEvent.CreatedAt, creationEvent.Name)
    {

    }


    public static TaskStatus Create(TaskStatusId id, DateTime createdAt, string name)
    {
        var taskStatus = new TaskStatus(id, createdAt, name);
        taskStatus.Enqueue(new TaskStatusCreatedEvent(id, createdAt, name));
        return taskStatus;
    }
}

public class TaskStatusCreatedEvent
{
    public TaskStatusId TaskStatusId { get; }
    public DateTime CreatedAt { get; }
    public string Name { get; }

    public TaskStatusCreatedEvent(TaskStatusId taskStatusId, DateTime createdAt, string name)
    {
        TaskStatusId = taskStatusId;
        CreatedAt = createdAt;
        Name = name;
    }
}

public static class TaskStatusWebApplicationExtensions
{
    public static async Task<WebApplication> EnsureDefaultTaskStatusExistsAsync(this WebApplication app)
    {
        var serviceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = serviceScopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore<TaskStatus, TaskStatusId>>();
        try
        {
            var existing = await eventStore.Find(TaskStatusId.Default, CancellationToken.None);
            if (existing is null)
            {
                await eventStore.Add(TaskStatus.Create(TaskStatusId.Default, DateTime.UtcNow, "Default"));
            }

        }
        catch (Exception e)
        {
            // Assume it's already there, possibly to some concurrency collision
            Console.WriteLine(e);
        }
        return app;
    }
}
