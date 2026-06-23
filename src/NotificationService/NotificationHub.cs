using Microsoft.AspNetCore.SignalR;

namespace NotificationService
{
    public class Notification
    {
        public string Message { get; set; }
        public int alarmPriority { get; set; }
    }

    public class NotificationHub : Hub
    {
        public async Task SendNotification(Notification message)
        {
            Console.WriteLine($"[NotificationHub] Sending notification: {message}");
            await Clients.All.SendAsync("ReceiveNotification", message);
        }

        public async Task SendSensorInactive(string sensorId)
        {
            Console.WriteLine($"[NotificationHub] Sensor inactive notification: {sensorId}");
            await Clients.All.SendAsync("SensorInactive", sensorId);
        }
    }
}