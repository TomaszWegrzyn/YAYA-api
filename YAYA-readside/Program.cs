using System.Text;
using System.Text.Json.Serialization;
using EventStore.Client;
using Microsoft.AspNetCore.Http.Json;
using YAYA_api;
using YAYA_readside;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet(
        "/RecentTasks",
        async (EventStoreClient eventStoreClient, CancellationToken cancellationToken) =>
        {
            var projection = await eventStoreClient
                .AggregateStreamForProjection<RecentTasksProjection>("$ce-Task", cancellationToken);
            return projection;
        })
    .WithName("RecentTasks")
    .WithOpenApi();

{

    var serviceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
    using var scope = serviceScopeFactory.CreateScope();
    var eventStoreClient = scope.ServiceProvider.GetRequiredService<EventStoreClient>();
    try
    {
        await eventStoreClient.SubscribeToAllAsync(
            FromAll.Start,
            async (subscription, @event, cancellationToken) =>
            {
                Console.WriteLine(@event.Event.EventType);
                Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data.Span));
            },
            cancellationToken: CancellationToken.None,
            filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents())
        );

    }
    catch (Exception e)
    {
        // Assume it's already there, possibly to some concurrency collision
        Console.WriteLine(e);
    }


}

app.Run();
