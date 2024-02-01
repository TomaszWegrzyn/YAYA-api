namespace YAYA_api;

public class TaskId: StronglyTypedValue<Guid>
{
    public TaskId(Guid value) : base(value)
    {
    }
}

public enum TaskStatus
{
    Open,
    InProgress,
    Done
}

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public class TaskCreatedEvent
{
    public TaskId TaskId { get; }
    public DateTime CreatedAt { get; }
    public TaskPriority TaskPriority { get; }

    public TaskCreatedEvent(TaskId taskId, DateTime createdAt, TaskPriority taskPriority)
    {
        TaskId = taskId;
        CreatedAt = createdAt;
        TaskPriority = taskPriority;
    }
}

public class TaskPriorityIncreasedEvent
{
    public TaskId TaskId { get; }

    public TaskPriorityIncreasedEvent(TaskId taskId)
    {
        TaskId = taskId;
    }
}


public class Task : Aggregate<TaskId, TaskCreatedEvent>
{
    private TaskPriority _taskPriority;
    private TaskStatus _taskStatus;

    private Task(TaskId id, DateTime createdAt, TaskPriority taskPriority) : base(id)
    {
        _taskPriority = taskPriority;
    }

    // we will call this using Activator.CreateInstance via reflection
    public Task(TaskCreatedEvent creationEvent) : this(creationEvent.TaskId, creationEvent.CreatedAt, creationEvent.TaskPriority)
    {
        
    }


    public static Task Create(TaskId id, DateTime createdAt, TaskPriority taskPriority)
    {
        var task = new Task(id, createdAt, taskPriority);
        task.Enqueue(new TaskCreatedEvent(id, createdAt, taskPriority));
        return task;
    }

    public void IncreasePriority()
    {
        _taskPriority = _taskPriority switch
        {
            TaskPriority.Low => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.High,
            TaskPriority.High => throw new InvalidOperationException("Task is already at highest priority"),
            _ => _taskPriority
        };

        Enqueue(new TaskPriorityIncreasedEvent(Id));
    }
}

