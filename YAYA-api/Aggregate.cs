namespace YAYA_api;

public interface IAggregate<out T, in TCreationEvent>
{
    T Id { get; }
    ulong Version { get; }

    object[] DequeueUncommittedEvents();
}

public abstract class Aggregate<T, TCreationEvent>: IAggregate<T, TCreationEvent> where T : notnull
{
    public T Id { get; protected set; }

    public ulong Version { get; protected set; }

    [NonSerialized] private readonly Queue<object> _uncommittedEvents = new();

    protected Aggregate(T id)
    {
        Id = id;
    }

    public virtual void When(object @event) { }

    public object[] DequeueUncommittedEvents()
    {
        var dequeuedEvents = _uncommittedEvents.ToArray();

        _uncommittedEvents.Clear();

        return dequeuedEvents;
    }

    protected void Enqueue(object @event)
    {
        _uncommittedEvents.Enqueue(@event);
    }
}