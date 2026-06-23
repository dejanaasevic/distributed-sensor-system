using Microsoft.AspNetCore.SignalR;

namespace NotificationService
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string message)
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