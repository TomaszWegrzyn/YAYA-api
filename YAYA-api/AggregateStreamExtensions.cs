using EventStore.Client;

namespace YAYA_api;


public static class AggregateStreamExtensions
{
    public static async Task<T?> AggregateStream<T>(
        this EventStoreClient eventStore,
        Guid id,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    ) where T : class, IProjection
    {
        var readResult = eventStore.ReadStreamAsync(
            Direction.Forwards,
            $"{nameof(T)}_{id}", // something like streamNameMapper would be better in the future
            fromVersion ?? StreamPosition.Start,
            cancellationToken: cancellationToken
        );

        if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        var aggregate = (T)Activator.CreateInstance(typeof(T), await readResult.FirstAsync(cancellationToken: cancellationToken))!;

        await foreach (var @event in readResult.Skip(1).WithCancellation(cancellationToken))
        {
            var eventData = @event.Deserialize();

            aggregate.When(eventData!);
        }

        return aggregate;
    }
}
