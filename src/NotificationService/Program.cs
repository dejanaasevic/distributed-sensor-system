using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyMethod()
                   .AllowAnyHeader()
                   .SetIsOriginAllowed(_ => true) // Critical for external IPs
                   .AllowCredentials(); // Required for SignalR
        });
});

var app = builder.Build();



app.UseCors("AllowAll");

app.MapHub<NotificationHub>("/notificationHub");
app.MapHealthChecks("/health");

app.Run();
