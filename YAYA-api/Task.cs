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
    Lowest,
    Low,
    Medium,
    High,
    Highest
}

public class Task : Aggregate<TaskId>
{
    private TaskPriority _taskPriority;
    private TaskStatus _taskStatus;

    private Task(TaskId id, DateTime createdAt, TaskPriority taskPriority, string? name) : base(id)
    {
        if(!Enum.IsDefined(taskPriority))
        {
            throw new ArgumentException($"Invalid task priority: {taskPriority}", nameof(taskPriority));
        }
        _taskPriority = taskPriority;
    }

    // we will call this using Activator.CreateInstance via reflection
    public Task(TaskCreatedEvent creationEvent) : 
        
        this(creationEvent.TaskId, creationEvent.CreatedAt, PriorityFromInt(creationEvent.TaskPriority), creationEvent.Name)
    {

    }

    // historically it was 0(Low), 1(Medium), 2(High) or other - out of range, but allowed due to a bug)
    private static TaskPriority PriorityFromInt(int value) 
    {
        return value switch
        {
            0 => TaskPriority.Low,
            2 => TaskPriority.High,
            _ => TaskPriority.Medium
        };
    }

    public Task(TaskCreatedEventV2 creationEvent) :
        this(creationEvent.TaskId, creationEvent.CreatedAt, creationEvent.TaskPriority, creationEvent.Name)
    {

    }


    public static Task Create(TaskId id, DateTime createdAt, TaskPriority taskPriority, string name)
    {
        var task = new Task(id, createdAt, taskPriority, name);
        task.Enqueue(new TaskCreatedEventV2(id, createdAt, taskPriority, name));
        return task;
    }

    public void IncreasePriority()
    {
        var taskPriorityIncreasedEvent = new TaskPriorityIncreasedEventV2(Id);
        Apply(taskPriorityIncreasedEvent);
        Enqueue(taskPriorityIncreasedEvent);
    }

    public void DecreasePriority()
    {
        var taskPriorityDecreasedEvent = new TaskPriorityDecreasedEventV2(Id);
        Apply(taskPriorityDecreasedEvent);
        Enqueue(taskPriorityDecreasedEvent);
    }

    public override void When(object @event)
    {
        switch (@event)
        {
            case TaskPriorityIncreasedEvent taskPriorityIncreased:
                Apply(taskPriorityIncreased);
                break;
            case TaskPriorityDecreasedEvent taskPriorityDecreased:
                Apply(taskPriorityDecreased);
                break;
            case TaskPriorityIncreasedEventV2 taskPriorityIncreased:
                Apply(taskPriorityIncreased);
                break;
            case TaskPriorityDecreasedEventV2 taskPriorityDecreased:
                Apply(taskPriorityDecreased);
                break;
            default:
                throw new InvalidOperationException($"Event type {@event.GetType().Name} can't be handled");
        }
    }

    private void Apply(TaskPriorityIncreasedEvent taskPriorityIncreased)
    {
        Version++;
        _taskPriority = _taskPriority switch
        {
            TaskPriority.Low => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.High,
            TaskPriority.High => TaskPriority.High,
            _ => throw new InvalidOperationException("Unknown task priority")
        };
    }


    private void Apply(TaskPriorityDecreasedEvent taskPriorityDecreased)
    {
        Version++;
        _taskPriority = _taskPriority switch
        {
            TaskPriority.High => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.Low,
            TaskPriority.Low => TaskPriority.Low,
            _ => throw new InvalidOperationException("Unknown task priority")
        };
    }

    private void Apply(TaskPriorityIncreasedEventV2 taskPriorityIncreased)
    {
        Version++;
        _taskPriority = _taskPriority switch
        {
            TaskPriority.Lowest => TaskPriority.Low,
            TaskPriority.Low => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.High,
            TaskPriority.High => TaskPriority.Highest,
            TaskPriority.Highest => throw new InvalidOperationException("Already highest priority"),

            _ => throw new InvalidOperationException("Unknown task priority")
        };
    }


    private void Apply(TaskPriorityDecreasedEventV2 taskPriorityDecreased)
    {
        Version++;
        _taskPriority = _taskPriority switch
        {
            TaskPriority.Highest => TaskPriority.High,
            TaskPriority.High => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.Low,
            TaskPriority.Low => TaskPriority.Lowest,
            TaskPriority.Lowest => throw new InvalidOperationException("Already lowest priority"),
            _ => throw new InvalidOperationException("Unknown task priority")
        };
    }
}


public class TaskCreatedEvent
{
    public TaskId TaskId { get; }
    public DateTime CreatedAt { get; }
    public int TaskPriority { get; } // historically it was 0(Low), 1(Medium), 2(High) or other - out of range, but allowed due to a bug)

    public string? Name { get; }

    public TaskCreatedEvent(TaskId taskId, DateTime createdAt, int taskPriority, string? name)
    {
        TaskId = taskId;
        CreatedAt = createdAt;
        TaskPriority = taskPriority;
        Name = name;
    }
}


public class TaskCreatedEventV2
{
    public TaskId TaskId { get; }
    public DateTime CreatedAt { get; }
    public TaskPriority TaskPriority { get; }

    public string? Name { get; }

    public TaskCreatedEventV2(TaskId taskId, DateTime createdAt, TaskPriority taskPriority, string? name)
    {
        TaskId = taskId;
        CreatedAt = createdAt;
        TaskPriority = taskPriority;
        Name = name;
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

public class TaskPriorityIncreasedEventV2
{
    public TaskId TaskId { get; }

    public TaskPriorityIncreasedEventV2(TaskId taskId)
    {
        TaskId = taskId;
    }
}

public class TaskPriorityDecreasedEvent
{
    public TaskId TaskId { get; }

    public TaskPriorityDecreasedEvent(TaskId taskId)
    {
        TaskId = taskId;
    }
}

public class TaskPriorityDecreasedEventV2
{
    public TaskId TaskId { get; }

    public TaskPriorityDecreasedEventV2(TaskId taskId)
    {
        TaskId = taskId;
    }
}




