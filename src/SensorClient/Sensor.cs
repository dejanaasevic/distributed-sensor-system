using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

        public DateTime? BlockedUntil { get; set; } = null;
        public bool IsRunning { get; set; } = false;

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

            string formattedString = $"[{mode}] {Config.FriendlyName} - {Math.Round(temperature, 2)}°C - MsgId: {MessageId} - {DateTime.UtcNow:HH:mm:ss}";
            ConsoleManager.WriteLog(formattedString, foreground, background);
        }

        public async Task RunAsync(HttpClient client, string serverUrl, CancellationToken token)
        {
            IsRunning = true;
            try
            {
                while (!token.IsCancellationRequested && IsRunning)
                {
                    if (BlockedUntil.HasValue)
                    {
                        var remainingSeconds = (BlockedUntil.Value - DateTime.UtcNow).TotalSeconds;
                        if (remainingSeconds > 0)
                        {
                            ConsoleManager.WriteLog($"[BLOCKED] {Config.FriendlyName} - Server rejection active. Cooldown: {Math.Ceiling(remainingSeconds)}s remaining...", ConsoleColor.Red);
                            await Task.Delay(1000, token);
                            continue;
                        }
                        else
                        {
                            BlockedUntil = null;
                            ConsoleManager.WriteLog($"[RESTORED] {Config.FriendlyName} - Server block expired. Resuming standard telemetry reporting.", ConsoleColor.Green);
                        }
                    }
                    if (IsInactivityAttack)
                    {
                        await Task.Delay(2000, token);
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

                    var internalPayload = new
                    {
                        Temperature = Math.Round(temperature, 2),
                        AlarmPriority = priority,
                        Quality = Config.Quality
                    };

                    string ciphertext;
                    string ivString;

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = Convert.FromBase64String(Config.SymmetricKey);
                        byte[] ivBytes = new byte[16];
                        using (var rng = new RNGCryptoServiceProvider()) { rng.GetBytes(ivBytes); }
                        aes.IV = ivBytes;
                        ivString = Convert.ToBase64String(aes.IV);

                        using var encryptor = aes.CreateEncryptor();
                        byte[] rawJsonBytes = JsonSerializer.SerializeToUtf8Bytes(internalPayload);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(rawJsonBytes, 0, rawJsonBytes.Length);
                        ciphertext = Convert.ToBase64String(encryptedBytes);
                    }

                    int msgIdToSend = MessageId++;
                    DateTime timestampToSend = DateTime.UtcNow;

                    if (IsReplayAttack)
                    {
                        msgIdToSend = 1;
                        timestampToSend = DateTime.UtcNow.AddMinutes(-10);
                        currentMode = "ATTACK: REPLAY";
                    }

                    string rawDataToSign = $"{Config.Id}:{msgIdToSend}:{timestampToSend:O}:{ciphertext}";
                    string signature;

                    if (IsBadSignatureAttack)
                    {
                        signature = "INVALID_TAMPERED_SIGNATURE_MOCK_DATA_XYZ=";
                        currentMode = "ATTACK: BAD_SIGNATURE";
                    }
                    else
                    {
                        using (var rsa = new RSACryptoServiceProvider())
                        {
                            rsa.FromXmlString(Config.PrivateKeyXml);
                            byte[] dataBytes = Encoding.UTF8.GetBytes(rawDataToSign);
                            byte[] signatureBytes = rsa.SignData(dataBytes, CryptoConfig.MapNameToOID("SHA256"));
                            signature = Convert.ToBase64String(signatureBytes);
                        }
                    }

                    try
                    {
                        if (TriggerFloodAttack)
                        {
                            PrintToConsole(temperature, priority, "ATTACK: DoS FLOOD");
                            ConsoleManager.WriteLog($"[!] Launching DoS Flood attack from {Config.FriendlyName}...", ConsoleColor.Red, ConsoleColor.DarkYellow);
                            for (int i = 0; i < 15; i++)
                            {
                                var floodMessage = new SensorMessage(Config.Id, MessageId++, DateTime.UtcNow, ivString, ciphertext, signature);
                                _ = client.PostAsJsonAsync(serverUrl, floodMessage);
                            }
                            TriggerFloodAttack = false;
                            BlockedUntil = DateTime.UtcNow.AddSeconds(30);
                            continue;
                        }
                        else
                        {
                            PrintToConsole(temperature, priority, currentMode);
                            var message = new SensorMessage(Config.Id, msgIdToSend, timestampToSend, ivString, ciphertext, signature);
                            await client.PostAsJsonAsync(serverUrl, message);
                        }
                    }
                    catch (HttpRequestException)
                    {
                        ConsoleManager.WriteLog($"[{Config.FriendlyName}] Error: Server unreachable, message dropped.", ConsoleColor.Gray);
                    }

                    await Task.Delay(_random.Next(1000, 5000), token);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation exit
            }
            finally
            {
                IsRunning = false;
                IsOutOfBoundsAttack = false;
                IsBadSignatureAttack = false;
                IsReplayAttack = false;
                IsInactivityAttack = false;
                TriggerFloodAttack = false;
            }
        }
    }
}