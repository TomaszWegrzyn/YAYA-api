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
         // WTF, this never returns
         
        var firstEventHandled = false;
        T? aggregate = null;

        await foreach (var @event in readResult)
        {
            var eventData = @event.Deserialize();
            if (firstEventHandled)
            {
                aggregate.When(eventData!);
            }
            else
            {
                aggregate = (T)Activator.CreateInstance(typeof(T), eventData)!;
                firstEventHandled = true;
            }
        }
        
        return aggregate;

        // if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
        //     return null;
        //
        // await foreach (var @event in readResult)
        // {
        //     var eventData = @event.Deserialize();
        //
        //     aggregate.When(eventData!);
        // }

    }
}
