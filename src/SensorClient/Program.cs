using SensorClient;
using System.Net.Http.Json;

var httpClient = new HttpClient();
var serverUrl = Environment.GetEnvironmentVariable("INGEST_URL") ?? "http://localhost:5001/api/ingest";

Random random = new Random();

double minTemperature = random.NextDouble() * 40.0 + 20.0;
double maxTemperature = random.NextDouble() * (100.0 - minTemperature) + minTemperature;
double alarmThreshold1 = random.NextDouble() * (maxTemperature - minTemperature) + minTemperature;
double alarmThreshold2 = random.NextDouble() * (maxTemperature - alarmThreshold1) + alarmThreshold1;
double alarmThreshold3 = random.NextDouble() * (maxTemperature - alarmThreshold2) + alarmThreshold2;

SensorConfig config = new SensorConfig(minTemperature, maxTemperature, alarmThreshold1, alarmThreshold2, alarmThreshold3, DataQuality.GOOD);
Sensor sensor = new Sensor(config);

if (!Console.IsOutputRedirected)
{
    try { Console.Clear(); } catch {}
}
try
{
    var response = await httpClient.PostAsJsonAsync($"{serverUrl}/register", sensor.Config);

    if (response.IsSuccessStatusCode)
    {
        var serverResponse = await response.Content.ReadFromJsonAsync<RegisterResponse>();

        if (serverResponse != null)
        {
            sensor.Config.FriendlyName = $"SENSOR-{serverResponse.Ordinal}";
        }

        Console.WriteLine($"Successfully registered! Server assigned identifier: {sensor.Config.FriendlyName}");
    }
    else
    {
        Console.WriteLine("Server rejected registration. Maximum threshold of 5 active nodes reached.");
        sensor.Config.FriendlyName = "SENSOR-REJECTED";
    }
}
catch (HttpRequestException)
{
    Console.WriteLine("Server is unreachable. Launching local isolated fallback mode.");
    sensor.Config.FriendlyName = "SENSOR-LOCAL";
}

Console.WriteLine("\nInitializing security dashboard layout viewport...");
Thread.Sleep(1500);

ConsoleManager.DrawMenuLayout(sensor.Config.FriendlyName);

_ = sensor.RunAsync(httpClient, serverUrl);

while (true)
{
    if (Console.IsInputRedirected)
    {
        await Task.Delay(Timeout.Infinite);
    }

    try
    {
        Console.SetCursorPosition(24, 8);
    }
    catch {}

    string? input = Console.ReadLine();

    if (input == null)
    {
        await Task.Delay(Timeout.Infinite);
    }

    if (input == "1")
    {
        sensor.IsOutOfBoundsAttack = true;
        ConsoleManager.WriteLog($"[ATTACK ENGAGED] {sensor.Config.FriendlyName} set to inject Out-of-Bounds metric (9999.9°C).", ConsoleColor.Magenta);
    }
    else if (input == "2")
    {
        sensor.IsBadSignatureAttack = true;
        ConsoleManager.WriteLog($"[ATTACK ENGAGED] {sensor.Config.FriendlyName} set to transmit Malformed Crypto Signatures.", ConsoleColor.Magenta);
    }
    else if (input == "3")
    {
        sensor.IsReplayAttack = true;
        ConsoleManager.WriteLog($"[ATTACK ENGAGED] {sensor.Config.FriendlyName} set to loop Stale sequence ID messages.", ConsoleColor.Magenta);
    }
    else if (input == "4")
    {
        sensor.IsInactivityAttack = true;
        ConsoleManager.WriteLog($"[ATTACK ENGAGED] {sensor.Config.FriendlyName} silenced. Simulating critical node failure drop.", ConsoleColor.Magenta);
    }
    else if (input == "5")
    {
        sensor.TriggerFloodAttack = true;
        ConsoleManager.WriteLog($"[ATTACK ENGAGED] {sensor.Config.FriendlyName} launching massive burst-rate Denial of Service (DoS) flood.", ConsoleColor.Magenta);
    }
    else if (input == "6")
    {
        sensor.IsOutOfBoundsAttack = false;
        sensor.IsBadSignatureAttack = false;
        sensor.IsReplayAttack = false;
        sensor.IsInactivityAttack = false;
        sensor.TriggerFloodAttack = false;
        ConsoleManager.WriteLog($"[SYSTEM RESTORED] All anomaly parameters mitigated for {sensor.Config.FriendlyName}.", ConsoleColor.Green);
    }
    else if (input == "7")
    {
        if (!Console.IsOutputRedirected)
        {
            try { Console.Clear(); } catch {}
        }
        Environment.Exit(0);
    }

    ConsoleManager.RefreshInputPrompt();
}

public class RegisterResponse
{
    public int Ordinal { get; set; }
}

public static class ConsoleManager
{
    private static readonly object _consoleLock = new object();
    private static int _currentLogLine = 14;
    private const int LogStartLine = 14;

    public static void DrawMenuLayout(string friendlyName)
    {
        lock (_consoleLock)
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine($"======================== ATTACK SIMULATION ({friendlyName}) ========================");
                return;
            }
            try
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"======================== ATTACK SIMULATION ({friendlyName}) ========================");
                Console.ResetColor();
                Console.WriteLine(" 1. Inject Out-of-Bounds Data Payload  | 5. Trigger DoS Burst Flood Attack");
                Console.WriteLine(" 2. Inject Tampered Cryptic Signatures | 6. Suppress Attacks (Restore Baseline)");
                Console.WriteLine(" 3. Inject Replay Outdated Sequence IDs| 7. Terminate Process Application");
                Console.WriteLine(" 4. Trigger Inactivity Silent Dropout  |");
                Console.WriteLine("-------------------------------------------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(" Notice: Dashboard menu remains fixed.");
                Console.ResetColor();
                Console.WriteLine();
                Console.Write("Select an option (1-7): ");
                Console.WriteLine();

                Console.SetCursorPosition(0, 11);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("=============================== LIVE MONITORING STREAM ===============================");
                Console.ResetColor();
            }
            catch {}
        }
    }

    public static void RefreshInputPrompt()
    {
        lock (_consoleLock)
        {
            if (Console.IsOutputRedirected) return;
            try
            {
                Console.SetCursorPosition(0, 8);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, 8);
                Console.Write("Select an option (1-7): ");
            }
            catch {}
        }
    }

    public static void WriteLog(string logMessage, ConsoleColor logColor, ConsoleColor backColor = ConsoleColor.Black)
    {
        lock (_consoleLock)
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(logMessage);
                return;
            }
            try
            {
                int preservedLeft = Console.CursorLeft;
                int preservedTop = Console.CursorTop;

                int maximumAllowedLogLine = Console.WindowHeight > 1 ? Console.WindowHeight - 1 : 28;

                if (_currentLogLine >= maximumAllowedLogLine)
                {
                    for (int i = LogStartLine; i <= maximumAllowedLogLine; i++)
                    {
                        Console.SetCursorPosition(0, i);
                        Console.Write(new string(' ', Console.WindowWidth - 1));
                    }
                    _currentLogLine = LogStartLine;
                }

                Console.SetCursorPosition(0, _currentLogLine);
                Console.ForegroundColor = logColor;
                Console.BackgroundColor = backColor;

                string paddedMessage = logMessage.PadRight(Console.WindowWidth - 1);
                Console.Write(paddedMessage);
                Console.ResetColor();

                _currentLogLine++;

                if (preservedTop == 8)
                {
                    Console.SetCursorPosition(preservedLeft, preservedTop);
                }
                else
                {
                    Console.SetCursorPosition(24, 8);
                }
            }
            catch
            {
                Console.WriteLine(logMessage);
            }
        }
    }
}