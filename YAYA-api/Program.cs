using EventStore.Client;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Json;
using YAYA_api;
using Task = YAYA_api.Task;

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
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();


app.AddTaskEndpoints();

app.Run();