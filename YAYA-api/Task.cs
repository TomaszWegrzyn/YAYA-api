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

public class TaskPriorityDecreasedEvent
{
    public TaskId TaskId { get; }

    public TaskPriorityDecreasedEvent(TaskId taskId)
    {
        TaskId = taskId;
    }
}

public class CreateTaskCommand
{
    public TaskPriority TaskPriority { get; }

    public CreateTaskCommand(TaskPriority taskPriority)
    {
        TaskPriority = taskPriority;
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

public class DesreaseTaskPriorityCommand
{
    public Guid TaskId { get; }

    public DesreaseTaskPriorityCommand(Guid taskId)
    {
        TaskId = taskId;
    }
}

public class Task : Aggregate<TaskId>
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
        var taskPriorityIncreasedEvent = new TaskPriorityIncreasedEvent(Id);
        Apply(taskPriorityIncreasedEvent);
        Enqueue(taskPriorityIncreasedEvent);
    }

    public void DecreasePriority()
    {
        var taskPriorityDecreasedEvent = new TaskPriorityDecreasedEvent(Id);
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
            default:
                throw new InvalidOperationException($"Event type {@event.GetType().Name} can't be handled");
        }
    }

    private void Apply(TaskPriorityIncreasedEvent taskPriorityIncreased)
    {
        _taskPriority = _taskPriority switch
        {
            TaskPriority.Low => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.High,
            TaskPriority.High => throw new InvalidOperationException("Task is already at highest priority"),
            _ => _taskPriority
        };
    }


    private void Apply(TaskPriorityDecreasedEvent taskPriorityDecreased)
    {
        _taskPriority = _taskPriority switch
        {
            TaskPriority.High => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.Low,
            TaskPriority.Low => throw new InvalidOperationException("Task is already at lowest priority"),
            _ => _taskPriority
        };
    }
}

