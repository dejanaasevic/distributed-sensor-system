using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<NotificationHub>("/notificationHub");

app.Run();
