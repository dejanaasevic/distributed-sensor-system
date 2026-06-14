using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string hubUrl = "http://localhost:5002/notificationHub";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        connection.On<string>("ReceiveNotification", (message) =>
        {
            Console.WriteLine($"[Alarm] {message}");
        });

        connection.On<string>("ReceiveTemperature", (message) =>
        {
            Console.WriteLine($"[Temperature] {message}");
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
}