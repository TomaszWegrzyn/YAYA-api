using EventStore.Client;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();



app.MapGet("/hello", () => "hello")
    .WithName("hello")
    .WithOpenApi();

app.MapPut("/putEvent/{stream}", async (CancellationToken cancellationToken, TestEvent testEvent, string stream) =>
    {
        const string connectionString = "esdb://eventstore.db:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000";
        // https://node1.eventstore:2113

        var settings = EventStoreClientSettings.Create(connectionString);

        var client = new EventStoreClient(settings);

        var eventData = new EventData(
            Uuid.NewUuid(),
            "TestEvent",
            JsonSerializer.SerializeToUtf8Bytes(testEvent)
        );

        await client.AppendToStreamAsync(
            stream,
            StreamState.Any,
            324
            new[] { eventData },
            cancellationToken: cancellationToken
        );
    })
    .WithName("putEvent")
    .WithOpenApi();

app.MapGet("/readEvent/{stream}", async (CancellationToken cancellationToken, string stream) =>
    {
        const string connectionString = "esdb://eventstore.db:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000";
        // https://node1.eventstore:2113

        var settings = EventStoreClientSettings.Create(connectionString);

        var client = new EventStoreClient(settings);

        var result = client.ReadStreamAsync(
            Direction.Forwards,
            stream,
            StreamPosition.Start,
            cancellationToken: cancellationToken
        );

        var events = await result.ToListAsync(cancellationToken);
        return events.Select(ev => JsonSerializer.Deserialize<TestEvent>(new MemoryStream(ev.Event.Data.ToArray())));
    })
    .WithName("readEvent")
    .WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record TestEvent
{
    public string Id { get; set; }
    public string ImportantData { get; set; }
}