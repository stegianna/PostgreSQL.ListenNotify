using PostgreSQL.ListenNotify.DemoWebAPI;
using PostgreSQL.ListenNotify.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Constants for PostgreSQL notification configuration
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")!;
const string channelName = "test_channel";

builder.Services.AddPostgresNotifications(options =>
{
    options.ConnectionString = connectionString;
    options.ListenChannels = new() { channelName };
    options.DefaultNotifyChannel = channelName;
    options.ApplicationName = "PostgreSQL.ListenNotify.DemoWebAPI";
});

builder.Services.AddSingleton<NotificationHandler>();

var app = builder.Build();

var notificationHandler = app.Services.GetRequiredService<NotificationHandler>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/notify", (string payload) =>
{
    notificationHandler.SendNotification(payload);
    return Results.Ok(new { message = "Notification sent", channel = channelName });
})
.WithName("Notify")
.WithDescription("Sends a notification to PostgreSQL channel")
.Produces<object>(StatusCodes.Status200OK)
.AddOpenApiOperationTransformer((operation, context, ct) =>
{
    operation.RequestBody ??= new Microsoft.OpenApi.OpenApiRequestBody();
    operation.RequestBody.Description = "The payload to send as notification";
    return Task.CompletedTask;
});

app.Run();
