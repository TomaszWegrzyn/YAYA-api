using EventStore.Client;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace YAYA_api;


public static class AggregateStreamExtensions
{

    public static async Task<T?> AggregateStreamReadingFromSnapshot<T, TId, TSnapshot>(
        this EventStoreClient eventStore,
        TId id,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    )
        where T : AggregateWithSnapshot<TId, TSnapshot> , IProjection
        where TSnapshot : class
        where TId : notnull

    {
        T aggregate;
        var snapshot = await eventStore.ReadSnapshot<TSnapshot, TId>(id, cancellationToken);
        if (snapshot is not null)
        {
            aggregate = (T)Activator.CreateInstance(typeof(T), snapshot)!;
        }

        var readResult = eventStore.ReadStreamAsync(
            Direction.Forwards,
            $"{typeof(T).Name}_{id}", // something like streamNameMapper would be better in the future
            fromVersion ?? StreamPosition.Start,
            cancellationToken: cancellationToken
        );
        if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        var firstEventHandled = false;
        // T? aggregate = null;
        //
        // await foreach (var @event in readResult)
        // {
        //     var eventData = @event.Deserialize();
        //     if (firstEventHandled)
        //     {
        //         aggregate.When(eventData!);
        //     }
        //     else
        //     {
        //         
        //         firstEventHandled = true;
        //     }
        // }

        return aggregate;

    }

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

    }

    public static async Task<T?> AggregateStreamForProjection<T>(
        this EventStoreClient eventStore,
        string stream,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    ) where T : class, IProjection, new()
    {
        var readResult = eventStore.ReadStreamAsync(
            Direction.Forwards,
            stream,
            fromVersion ?? StreamPosition.Start,
            cancellationToken: cancellationToken,
            resolveLinkTos: true
        );
        if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        var projection = new T();

        await foreach (var @event in readResult)
        {
            var eventData = @event.Deserialize();

            projection.When(eventData!);
        }

        return projection;
    }
}
