using EventStore.Client;

namespace YAYA_api;


public interface IEventStore<T, TId> where T : Aggregate<TId> where TId : notnull
{
    Task<T?> Find(Guid id, CancellationToken cancellationToken);
    Task<ulong> Add(T aggregate, CancellationToken ct = default);
    Task<ulong> Update(T aggregate, ulong? expectedRevision = null, CancellationToken ct = default);
    Task<ulong> Delete(T aggregate, ulong? expectedRevision = null, CancellationToken ct = default);
}


public class EventStore<T, TId>: IEventStore<T, TId>where T : Aggregate<TId> where TId : notnull
{
    private readonly EventStoreClient eventStore;

    public EventStore(
        EventStoreClient eventStore
    )
    {
        this.eventStore = eventStore;
    }

    public Task<T?> Find(Guid id, CancellationToken cancellationToken) =>
        eventStore.AggregateStream<T>(
            id,
            cancellationToken
        ); // use it now - TODO

    public async Task<ulong> Add(T aggregate, CancellationToken ct = default)
    {
        var result = await eventStore.AppendToStreamAsync(
            StreamNameMapper.ToStreamId<T>(aggregate.Id), 
            StreamState.NoStream,
            GetEventsToStore(aggregate),
            cancellationToken: ct
        ).ConfigureAwait(false);

        return result.NextExpectedStreamRevision.ToUInt64();
    }

    public async Task<ulong> Update(T aggregate, ulong? expectedRevision = null, CancellationToken ct = default)
    {
        var eventsToAppend = GetEventsToStore(aggregate);
        var nextVersion = expectedRevision ?? (ulong)(aggregate.Version - eventsToAppend.Count);

        var result = await eventStore.AppendToStreamAsync(
            StreamNameMapper.ToStreamId<T>(aggregate.Id),
            nextVersion,
            eventsToAppend,
            cancellationToken: ct
        ).ConfigureAwait(false);

        return result.NextExpectedStreamRevision.ToUInt64();
    }

    public Task<ulong> Delete(T aggregate, ulong? expectedRevision = null, CancellationToken ct = default) =>
        Update(aggregate, expectedRevision, ct);

    private static List<EventData> GetEventsToStore(T aggregate)
    {
        var events = aggregate.DequeueUncommittedEvents();

        return events
            .Select(@event => @event.ToJsonEventData(TelemetryPropagator.GetPropagationContext()))
            .ToList();
    }
}
