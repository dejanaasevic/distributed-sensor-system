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

        public bool IsOutOfBoundsAttack { get; set; } = false;
        public bool IsBadSignatureAttack { get; set; } = false;
        public bool IsReplayAttack { get; set; } = false;
        public bool IsInactivityAttack { get; set; } = false;
        public bool TriggerFloodAttack { get; set; } = false;

        private readonly Random _random = new Random();

        public Sensor(SensorConfig config)
        {
            Config = config;
        }

        public int CheckAlarm(double temperature)
        {
            if (temperature >= Config.AlarmThreshold3) return 3;
            if (temperature >= Config.AlarmThreshold2) return 2;
            if (temperature >= Config.AlarmThreshold1) return 1;
            return 0;
        }

        public void PrintToConsole(double temperature, int priority, string mode)
        {
            ConsoleColor foreground = ConsoleColor.White;
            ConsoleColor background = ConsoleColor.Black;

            switch (priority)
            {
                case 0: foreground = ConsoleColor.White; break;
                case 1: foreground = ConsoleColor.Yellow; break;
                case 2: foreground = ConsoleColor.DarkYellow; break;
                case 3: foreground = ConsoleColor.Red; break;
            }

            if (mode != "NORMAL")
            {
                background = ConsoleColor.DarkRed;
            }

            string formattedString = $"[{mode}] {Config.Id} - {Math.Round(temperature, 2)}°C - MsgId: {MessageId} - {DateTime.UtcNow:HH:mm:ss}";
            ConsoleManager.WriteLog(formattedString, foreground, background);
        }

        public async Task RunAsync(HttpClient client, string serverUrl)
        {
            while (true)
            {
                if (IsInactivityAttack)
                {
                    await Task.Delay(2000);
                    continue;
                }

                double temperature = Config.MinTemperature + _random.NextDouble() * (Config.MaxTemperature - Config.MinTemperature);
                string currentMode = "NORMAL";

                if (IsOutOfBoundsAttack)
                {
                    temperature = 9999.9;
                    currentMode = "ATTACK: OUT_OF_BOUNDS";
                }

                int priority = CheckAlarm(temperature);

                int msgIdToSend = MessageId++;
                if (IsReplayAttack)
                {
                    msgIdToSend = 1;
                    currentMode = "ATTACK: REPLAY";
                }

                string signature = "VALID_PARTNER_RSA_SIGNATURE_MOCK";
                if (IsBadSignatureAttack)
                {
                    signature = "INVALID_TAMPERED_SIGNATURE";
                    currentMode = "ATTACK: BAD_SIGNATURE";
                }

                PrintToConsole(temperature, priority, currentMode);
                try
                {
                    if (TriggerFloodAttack)
                    {
                        ConsoleManager.WriteLog($"[!] Launching DoS Flood attack from {Config.Id}...", ConsoleColor.Red, ConsoleColor.DarkYellow);
                        for (int i = 0; i < 15; i++)
                        {
                            var floodMessage = new SensorMessage(Config.Id, temperature, DateTime.UtcNow, priority, Config.Quality, MessageId++, signature);
                            _ = client.PostAsJsonAsync(serverUrl, floodMessage);
                        }
                        TriggerFloodAttack = false;
                    }
                    else
                    {
                        var message = new SensorMessage(Config.Id, temperature, DateTime.UtcNow, priority, Config.Quality, msgIdToSend, signature);
                        await client.PostAsJsonAsync(serverUrl, message);
                    }
                }
                catch (HttpRequestException)
                {
                    ConsoleManager.WriteLog($"[{Config.Id}] Error: Server unreachable, cached entry skipped.", ConsoleColor.Gray);
                }

                await Task.Delay(_random.Next(1000, 10000));
            }
        }
    }
}