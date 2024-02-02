using EventStore.Client;
using System.Text;
using System.Text.Json;

namespace YAYA_api;

public static class EventStoreDBSerializer
{

    static EventStoreDBSerializer()
    {

    }

    public static T? Deserialize<T>(this ResolvedEvent resolvedEvent) where T : class =>
        Deserialize(resolvedEvent) as T;

    public static object? Deserialize(this ResolvedEvent resolvedEvent)
    {
        // get type
        var eventType = ByName(resolvedEvent.Event.EventType);

        if (eventType == null)
            return null;

        // deserialize event
        return JsonSerializer.Deserialize(
            Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
            eventType
        )!;
    }
    
    public static EventData ToJsonEventData(this object @event, object? metadata = null) =>
        new(
            Uuid.NewUuid(),
            @event.GetType().FullName,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event)),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata ?? new { }))
        );

    // should be some util
    public static Type? ByName(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
        {
            var tt = assembly.GetType(name);
            if (tt != null)
            {
                return tt;
            }
        }

        return null;
    }
}