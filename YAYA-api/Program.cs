using EventStore.Client;
using System.Text.Json;
using System.Threading.Tasks;
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

app.MapPost(
        "/IncreasePriority/",
        async (IncreaseTaskPriorityCommand command, IEventStore<Task, TaskId> eventStore, CancellationToken cancellationToken) =>
        {
            var task = await eventStore.Find(new TaskId(command.TaskId), cancellationToken);
            task.IncreasePriority();
            await eventStore.Update(task, ct: cancellationToken);
        })
    .WithName("IncreasePriority")
    .WithOpenApi();

app.MapPost(
        "/DecreasePriority/",
        async (DecreaseTaskPriorityCommand command, IEventStore<Task, TaskId> eventStore, CancellationToken cancellationToken) =>
        {
            var task = await eventStore.Find(new TaskId(command.TaskId), cancellationToken);
            task.DecreasePriority();
            await eventStore.Update(task, ct: cancellationToken);
        })
    .WithName("DecreasePriority")
    .WithOpenApi();


app.Run();