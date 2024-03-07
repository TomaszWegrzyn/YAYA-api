using YAYA_api;

namespace YAYA_readside;

public class RecentTask
{
    public RecentTask(TaskId taskId, string? name, DateTime createdAt)
    {
        TaskId = taskId;
        Name = name;
        CreatedAt = createdAt;
    }

    public TaskId TaskId { get; }

    public string? Name { get; }
    public DateTime CreatedAt { get; }
}

public class RecentTasksProjection : IProjection
{
    private readonly Stack<RecentTask> _recentTasks = new Stack<RecentTask>();
    private const int MaxTasksToShow = 10;

    public IReadOnlyCollection<RecentTask> RecentTasks => _recentTasks;

    public void When(object @event)
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
            case TaskCreatedEvent taskCreated:
                Apply(taskCreated);
                break;
            case TaskCreatedEventV2 taskCreated:
                Apply(taskCreated);
                break;
            default:
                throw new InvalidOperationException(
                    $"Event type {@event.GetType().Name} can't be handled");
        }
    }

    private void Apply(TaskCreatedEventV2 taskCreatedEvent)
    {
        if (_recentTasks.Count == MaxTasksToShow)
        {
            _recentTasks.Pop();
        }

        _recentTasks.Push(new RecentTask(taskCreatedEvent.TaskId, taskCreatedEvent.Name,
            taskCreatedEvent.CreatedAt));
    }

    private void Apply(TaskCreatedEvent taskCreatedEvent)
    {
        if (_recentTasks.Count == MaxTasksToShow)
        {
            _recentTasks.Pop();
        }

        _recentTasks.Push(new RecentTask(taskCreatedEvent.TaskId, taskCreatedEvent.Name,
            taskCreatedEvent.CreatedAt));
    }

    private void Apply(TaskPriorityIncreasedEvent taskPriorityIncreased)
    {
        // ignore this event since we don't care about it
    }


    private void Apply(TaskPriorityDecreasedEvent taskPriorityDecreased)
    {
        // ignore this event since we don't care about it
    }

    private void Apply(TaskPriorityIncreasedEventV2 taskPriorityIncreased)
    {
        // ignore this event since we don't care about it

    }


    private void Apply(TaskPriorityDecreasedEventV2 taskPriorityDecreased)
    {
        // ignore this event since we don't care about it

    }
}

