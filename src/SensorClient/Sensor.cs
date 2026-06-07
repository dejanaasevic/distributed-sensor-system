using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SensorClient
{
    public class Sensor
    {
        public SensorConfig Config { get; set; }
        public int MessageId { get; set; } = 0;

        private readonly Random _random = new Random();

        public Sensor(SensorConfig config)
        {
            Config = config;
        }

        // method to calculate the alarm priority
        public int CheckAlarm(double temperature)
        {
            if (temperature >= Config.AlarmThreshold3)
            {
                return 3;
            }
            else if (temperature >= Config.AlarmThreshold2)
            {
                return 2;

            }
            else if (temperature >= Config.AlarmThreshold1)
            {
                return 1;
            }
            return 0;
        }

        // method to print the message to the console
        public void PrintToConsole(double temperature, int priority)
        {
            switch (priority)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
            Console.WriteLine($"{Config.Id} - {Math.Round(temperature, 2)}°C - {DateTime.UtcNow}");
            Console.ResetColor();
        }

        // loop to generate random temperature readings and send them to the server
        public async Task RunAsync(HttpClient client, string serverUrl)
        {
            while (true)
            {
                double temperature = Config.MinTemperature + _random.NextDouble() * (Config.MaxTemperature - Config.MinTemperature);
                int priority = CheckAlarm(temperature);
                PrintToConsole(temperature, priority);
                SensorMessage message = new SensorMessage(Config.Id, temperature, DateTime.UtcNow, priority, Config.Quality, MessageId++);
                try
                {
                    await client.PostAsJsonAsync(serverUrl, message);
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine($"[{Config.Id}] server unavailable, continuing...");
                }
                await Task.Delay(_random.Next(1000, 10000));
            }
        }
    }
}