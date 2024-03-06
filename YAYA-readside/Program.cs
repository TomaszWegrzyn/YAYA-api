using System.Text;
using System.Text.Json.Serialization;
using EventStore.Client;
using Microsoft.AspNetCore.Http.Json;
using YAYA_api;
using YAYA_readside;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

app.Run();
