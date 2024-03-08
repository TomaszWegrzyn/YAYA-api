using EventStore.Client;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using YAYA_api;
using Task = YAYA_api.Task;
using TaskStatus = YAYA_api.TaskStatus;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new StronglyTypedValueJsonConverter<TaskId>());
    options.SerializerOptions.Converters.Add(new StronglyTypedValueJsonConverter<TaskStatusId>());
});

// needed for swagger
// boooo!
builder.Services.Configure<MvcJsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new StronglyTypedValueJsonConverter<TaskId>());
    options.JsonSerializerOptions.Converters.Add(new StronglyTypedValueJsonConverter<TaskStatusId>());
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

app.Run();

async Task SubscribeToTaskEvents(WebApplication webApplication)
{
    var serviceScopeFactory = webApplication.Services.GetRequiredService<IServiceScopeFactory>();
    using var scope = serviceScopeFactory.CreateScope();
    var eventStoreClient = scope.ServiceProvider.GetRequiredService<EventStoreClient>();
    try
    {
        await eventStoreClient.SubscribeToAllAsync(
            FromAll.Start,
            async (subscription, @event, cancellationToken) =>
            {
                var serviceScopeFactory = webApplication.Services.GetRequiredService<IServiceScopeFactory>();
                using var scope = serviceScopeFactory.CreateScope();
                var taskEventStore = scope.ServiceProvider.GetRequiredService<IEventStore<Task, TaskId>>();


                Console.WriteLine(@event.Event.EventType);
                Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.Span));
            },
            cancellationToken: CancellationToken.None,
            filterOptions: new SubscriptionFilterOptions(StreamFilter.Prefix("Task_"))
        );
    }
    catch (Exception e)
    {
        Console.WriteLine("Could not subscribe", e);
    }
}
