using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string hubUrl = "http://192.168.1.144:8080/notificationHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        connection.On<Notification>("ReceiveNotification", (notification) =>
        {
            switch (notification.alarmPriority)
            {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ResetColor();
                    break;
            }
            Console.WriteLine($"{notification.Message}");
            Console.ResetColor();
        });

        try
        {
            await connection.StartAsync();
            Console.WriteLine("Connected to Notification Service. Waiting for messages...");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to Notification Service: {ex.Message}");
        }
    }

    class Notification
    {
        public string Message { get; set; }
        public int alarmPriority { get; set; }
    }
}