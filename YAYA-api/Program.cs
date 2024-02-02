using EventStore.Client;
using System.Text.Json;
using YAYA_api;
using Task = YAYA_api.Task;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string connectionString = "esdb://eventstore.db:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000";
builder.Services.AddSingleton(
    new EventStoreClient(EventStoreClientSettings.Create(connectionString)));
builder.Services.AddScoped<IEventStore<Task, TaskId>, EventStore<Task, TaskId>>();
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

// app.MapPut("/putEvent/{stream}", async (CancellationToken cancellationToken, TestEvent testEvent, string stream) =>
//     {
//         const string connectionString = "esdb://eventstore.db:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000";
//         // https://node1.eventstore:2113
//
//         var settings = EventStoreClientSettings.Create(connectionString);
//
//         var client = new EventStoreClient(settings);
//
//         var eventData = new EventData(
//             Uuid.NewUuid(),
//             "TestEvent",
//             JsonSerializer.SerializeToUtf8Bytes(testEvent)
//         );
//
//         await client.AppendToStreamAsync(
//             stream,
//             StreamState.Any,
//             324
//             new[] { eventData },
//             cancellationToken: cancellationToken
//         );
//     })
//     .WithName("putEvent")
//     .WithOpenApi();

/*
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
*/

app.MapPost(
        "/CreateTask/", 
        async (CreateTaskCommand command, IEventStore<Task, TaskId> eventStore) =>
    {
        await eventStore.Add(Task.Create(new TaskId(Guid.NewGuid()), DateTime.Now, command.TaskPriority));
    })
    .WithName("CreateTask")
    .WithOpenApi();

app.MapGet(
        "/GetTasks/{id}",
        async (Guid id, IEventStore<Task, TaskId> eventStore, CancellationToken cancellationToken) => await eventStore.Find(new TaskId(id), cancellationToken))
    .WithName("GetTask")
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