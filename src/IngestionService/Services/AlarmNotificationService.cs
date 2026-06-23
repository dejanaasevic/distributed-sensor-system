using Microsoft.AspNetCore.SignalR.Client;

namespace IngestionService.Services
{
    public interface IAlarmNotificationService
    {
        Task SendNotificationAsync(string message, int alarmPriority = 0);
        Task SendSensorInactiveAsync(string sensorId);
    }

    public class AlarmNotificationService : IAlarmNotificationService, IAsyncDisposable
    {
        private readonly HubConnection _connection;

        public AlarmNotificationService(IConfiguration configuration)
        {
            // Read the URL from configuration (e.g., http://notification:5002/notificationHub)
            string hubUrl = configuration["NotificationHubUrl"] ?? "http://notification:8080/notificationHub";

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect() // Automatically handles dropouts
                .Build();
        }

        public async Task SendNotificationAsync(string message, int alarmPriority = 0)
        {
            // Ensure the connection is started before sending
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
            }

            // "SendNotification" matches the method name inside your NotificationHub.cs
            await _connection.InvokeAsync("SendNotification", new Notification { Message = message, alarmPriority = alarmPriority });
        }

        public async Task SendSensorInactiveAsync(string sensorId)
        {
            // Ensure the connection is started before sending
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
            }

            // "SendSensorInactive" matches the method name inside your NotificationHub.cs
            await _connection.InvokeAsync("SendSensorInactive", sensorId);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }

    class Notification
    {
        public string Message { get; set; }
        public int alarmPriority { get; set; }
    }
}