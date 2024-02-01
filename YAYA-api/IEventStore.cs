using EventStore.Client;

namespace YAYA_api;


public interface IEventStore<T, TId> where T : Aggregate<TId>
{

    Task<>
}