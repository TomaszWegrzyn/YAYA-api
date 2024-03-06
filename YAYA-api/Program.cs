using EventStore.Client;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using YAYA_api;
using Task = YAYA_api.Task;
using TaskStatus = YAYA_api.TaskStatus;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

const string connectionString = "esdb://eventstore.db:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000";
builder.Services.AddSingleton(
    new EventStoreClient(EventStoreClientSettings.Create(connectionString)));

builder.Services.AddScoped<IEventStore<Task, TaskId>, EventStore<Task, TaskId>>();
builder.Services.AddScoped<IEventStore<TaskStatus, TaskStatusId>, EventStore<TaskStatus, TaskStatusId>>();
builder.Services.AddScoped<IEventStore<TaskStatusNameLock, string>, EventStore<TaskStatusNameLock, string>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();


app.AddTaskEndpoints();
app.AddTaskStatusEndpoints();
await app.EnsureDefaultTaskStatusExistsAsync();

// app.MapGet(
//         "/Projection-Test-OutDated/",
//         async (EventStoreClient eventStoreClient, CancellationToken cancellationToken) =>
//         {
//             var readResult = eventStoreClient.ReadStreamAsync(
//                 Direction.Forwards,
//                 "$category-Task",
//                 StreamPosition.Start,
//                 cancellationToken: cancellationToken
//             );
//             if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
//                 return null;
//
//             var result = new List<object>();
//             await foreach (var @event in readResult)
//             {
//                 result.Add(Encoding.UTF8.GetString(@event.Event.Data.ToArray()));
//                 result.Add(Encoding.UTF8.GetString(@event.Event.Metadata.ToArray()));
//                 result.Add(Encoding.UTF8.GetString(@event.OriginalEvent.Data.ToArray()));
//                 result.Add(Encoding.UTF8.GetString(@event.OriginalEvent.Metadata.ToArray()));
//             }
//             return result;
//         })
//     .WithName("Projection")
//     .WithOpenApi();


app.Run();