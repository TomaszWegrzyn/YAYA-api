using EventStore.Client;

namespace YAYA_api;


public static class SnapshotStreamExtensions
{
    public static async Task<TSnapshot?> ReadSnapshot<TSnapshot, TId>(
        this EventStoreClient eventStore,
        TId id,
        CancellationToken cancellationToken
    ) 
        where TId : notnull 
        where TSnapshot : class
    {
        var readResult = eventStore.ReadStreamAsync(
            Direction.Backwards,
            $"{typeof(TSnapshot).Name}_{id}", // something like streamNameMapper would be better in the future
            StreamPosition.End,
            cancellationToken: cancellationToken
        );
        if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            return null;

        var lastSnapshot = readResult.GetAsyncEnumerator(cancellationToken).Current;

        return lastSnapshot.Deserialize<TSnapshot>();
    }
}
