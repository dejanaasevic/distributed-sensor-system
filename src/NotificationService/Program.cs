using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHub<NotificationHub>("/notificationHub");
app.MapHealthChecks("/health");

app.Run();
