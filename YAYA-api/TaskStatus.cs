using Microsoft.Extensions.DependencyInjection;

namespace YAYA_api;

public class TaskStatusId : StronglyTypedValue<Guid>
{
    public TaskStatusId(Guid value) : base(value)
    {
    }

    public static readonly TaskStatusId Default = new TaskStatusId(new Guid("8f95336d-c662-41f0-8007-36debeb0ba75"));

}

// TODO Add a name uniqueness logic(probably needs a saga to insert name to separate DB and remove it if creation fails)
// Alternatively, we could use reservation pattern, where we reserve a nme for few minutes and if it's not used, we can use it. If we fail the creation, we can remove the reservation. Which also needs a saga...
// Alternatively, we can use event store with names as stream ids to record "reservations of names". But is ALSO needs a saga... FCK 


// Alternatively, we can just be eventually consistent and allow duplicates, but then we need to add some but to detect duplicates and "handle" it(whatever it means)
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
        using IServiceScope scope = serviceScopeFactory.CreateScope();
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
