using EventStore.Client;

namespace YAYA_api;


public static class AggregateStreamExtensions
{
    public static async Task<T?> AggregateStream<T, TId>(
        this EventStoreClient eventStore,
        TId id,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    ) where T : class, IProjection where TId : notnull
    {
        var readResult = eventStore.ReadStreamAsync(
            Direction.Forwards,
            $"{typeof(T).Name}_{id}", // something like streamNameMapper would be better in the future
            fromVersion ?? StreamPosition.Start,
            cancellationToken: cancellationToken
        );

        if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        var firstEvent = (await readResult.FirstAsync(cancellationToken: cancellationToken)).Deserialize();
        var aggregate = (T)Activator.CreateInstance(typeof(T), firstEvent)!;

        await foreach (var @event in readResult.Skip(1).WithCancellation(cancellationToken))
        {
            var eventData = @event.Deserialize();

            aggregate.When(eventData!);
        }

        return aggregate;
    }
}
