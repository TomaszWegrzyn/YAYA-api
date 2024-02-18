using Microsoft.AspNetCore.Http.HttpResults;
using System.Threading.Tasks;

namespace YAYA_api;

public class TaskStatusNameLock : Aggregate<string>
{
    private DateTime _lastAcquireStartTime;
    private const ushort MaxSecondsToAcquire = 10;
    public bool Locked { get; private set; }

    private TaskStatusNameLock(string name) : base(name)
    {
    }

    public TaskStatusNameLock(TaskStatusNameLockCreatedEvent creationEvent) :
        this(creationEvent.TaskStatusName)
    {
    }

    public static TaskStatusNameLock Create(string name)
    {
        var taskStatusNameLock = new TaskStatusNameLock(name);
        taskStatusNameLock.Enqueue(new TaskStatusNameLockCreatedEvent(name));
        return taskStatusNameLock;
    }

    public void StartAcquire(DateTime currentTime)
    {
        var taskStatusNameLockAcquireStartedEvent = new TaskStatusNameLockAcquireStartedEvent(currentTime);
        Apply(taskStatusNameLockAcquireStartedEvent);
        Enqueue(taskStatusNameLockAcquireStartedEvent);
    }

    public void FinishAcquire(DateTime currentTime)
    {
        var taskStatusNameLockAcquireStartedEvent = new TaskStatusNameLockAcquireFinishedEvent(currentTime);
        Apply(taskStatusNameLockAcquireStartedEvent);
        Enqueue(taskStatusNameLockAcquireStartedEvent);
    }

    public void Release()
    {

        var taskStatusNameLockReleasedEvent = new TaskStatusNameLockReleasedEvent();
        Apply(taskStatusNameLockReleasedEvent);
        Enqueue(taskStatusNameLockReleasedEvent);
    }

    private void Apply(TaskStatusNameLockAcquireStartedEvent taskStatusNameLockAcquireStartedEvent)
    {
        if (Locked)
        {
            throw new InvalidOperationException("Lock is already acquired");
        }

        if (_lastAcquireStartTime >= taskStatusNameLockAcquireStartedEvent.Time)
        {
            throw new InvalidOperationException("Acquire start time can't be earlier than the last acquire start time");
        }

        if (IsAcquireInProgress(taskStatusNameLockAcquireStartedEvent.Time))
        {
            throw new InvalidOperationException("Acquire already in progress");
        }
        {                    }
        Version++;
        _lastAcquireStartTime = taskStatusNameLockAcquireStartedEvent.Time;
    }

    private void Apply(TaskStatusNameLockAcquireFinishedEvent taskStatusNameLockAcquireStartedEvent)
    {
        if (Locked)
        {
            throw new InvalidOperationException("Lock is already acquired");
        }

        if (!IsAcquireInProgress(taskStatusNameLockAcquireStartedEvent.Time))
        {
            throw new InvalidOperationException("Acquire was not started, or timed out");
        }
        Version++;
        _lastAcquireStartTime = taskStatusNameLockAcquireStartedEvent.Time;
        Locked = true;
    }

    private void Apply(TaskStatusNameLockReleasedEvent taskStatusNameLockReleasedEvent)
    {
        if (!Locked)
        {
            throw new InvalidOperationException("Lock is not acquired");
        }

        Version++;
        Locked = false;
    }

    private bool IsAcquireInProgress(DateTime currentTime)
    {
        return !Locked && SecondsSinceAcquireStart(currentTime) <= MaxSecondsToAcquire;
    }

    private double SecondsSinceAcquireStart(DateTime currentTime)
    {
        return currentTime.Subtract(_lastAcquireStartTime).TotalSeconds;
    }

    public override void When(object @event)
    {
        switch (@event)
        {
            case TaskStatusNameLockAcquireStartedEvent taskStatusNameLockAcquireStartedEvent:
                Apply(taskStatusNameLockAcquireStartedEvent);
                break;
            case TaskStatusNameLockAcquireFinishedEvent taskStatusNameLockAcquireFinishedEvent:
                Apply(taskStatusNameLockAcquireFinishedEvent);
                break;
            case TaskStatusNameLockReleasedEvent taskStatusNameLockReleasedEvent:
                Apply(taskStatusNameLockReleasedEvent);
                break;
            default:
                throw new InvalidOperationException($"Event type {@event.GetType().Name} can't be handled");
        }
    }
}

public class TaskStatusNameLockCreatedEvent
{
    public string TaskStatusName { get; }

    public TaskStatusNameLockCreatedEvent(string taskStatusName)
    {
        TaskStatusName = taskStatusName;
    }
}

public class TaskStatusNameLockAcquireStartedEvent
{
    public TaskStatusNameLockAcquireStartedEvent(DateTime time)
    {
        Time = time;
    }

    public DateTime Time { get; }
}

public class TaskStatusNameLockAcquireFinishedEvent
{
    public TaskStatusNameLockAcquireFinishedEvent(DateTime time)
    {
        Time = time;
    }

    public DateTime Time { get; }
}

public class TaskStatusNameLockReleasedEvent
{

}

