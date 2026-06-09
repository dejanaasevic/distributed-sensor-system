using SensorClient;
using System.Net.Http.Json;

var httpClient = new HttpClient();
var serverUrl = "http://localhost:5001/api/ingest";

Random random = new Random();
var sensors = new List<Sensor>();

for (int i = 1; i <= 5; i++)
{
    string id = $"SENSOR-{i}";
    double minTemperature = random.NextDouble() * 40.0 + 20.0; // Realistic values between 20°C and 60°C
    double maxTemperature = random.NextDouble() * (100.0 - minTemperature) + minTemperature;
    double alarmThreshold1 = random.NextDouble() * (maxTemperature - minTemperature) + minTemperature;
    double alarmThreshold2 = random.NextDouble() * (maxTemperature - alarmThreshold1) + alarmThreshold1;
    double alarmThreshold3 = random.NextDouble() * (maxTemperature - alarmThreshold2) + alarmThreshold2;

    // The constructor handles high-entropy CSPRNG cryptographic key generation automatically
    SensorConfig config = new SensorConfig(id, minTemperature, maxTemperature, alarmThreshold1, alarmThreshold2, alarmThreshold3, DataQuality.GOOD);
    Sensor sensor = new Sensor(config);
    sensors.Add(sensor);
}

Console.Clear();
foreach (var sensor in sensors)
{
    try
    {
        // Transmits configuration containing SymmetricKey and PublicKeyXml to the server database
        await httpClient.PostAsJsonAsync($"{serverUrl}/register", sensor.Config);
        Console.WriteLine($"{sensor.Config.Id} registered with secure keys successfully.");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine($"{sensor.Config.Id} registration failed.");
    }
}

Console.WriteLine("\nInitializing security dashboard layout viewport...");
Thread.Sleep(1500);

ConsoleManager.DrawMenuLayout();

foreach (var sensor in sensors)
{
    _ = sensor.RunAsync(httpClient, serverUrl);
}

while (true)
{
    Console.SetCursorPosition(24, 8);
    string input = Console.ReadLine();

    if (input == "1")
    {
        sensors[0].IsOutOfBoundsAttack = true;
        ConsoleManager.WriteLog("[ATTACK ENGAGED] SENSOR-1 set to inject Out-of-Bounds metric (9999.9°C).", ConsoleColor.Magenta);
    }
    else if (input == "2")
    {
        sensors[1].IsBadSignatureAttack = true;
        ConsoleManager.WriteLog("[ATTACK ENGAGED] SENSOR-2 set to transmit Malformed Crypto Signatures.", ConsoleColor.Magenta);
    }
    else if (input == "3")
    {
        sensors[2].IsReplayAttack = true;
        ConsoleManager.WriteLog("[ATTACK ENGAGED] SENSOR-3 set to loop Stale sequence ID messages.", ConsoleColor.Magenta);
    }
    else if (input == "4")
    {
        sensors[3].IsInactivityAttack = true;
        ConsoleManager.WriteLog("[ATTACK ENGAGED] SENSOR-4 silenced. Simulating critical node failure drop.", ConsoleColor.Magenta);
    }
    else if (input == "5")
    {
        sensors[4].TriggerFloodAttack = true;
        ConsoleManager.WriteLog("[ATTACK ENGAGED] SENSOR-5 launching massive burst-rate Denial of Service (DoS) flood.", ConsoleColor.Magenta);
    }
    else if (input == "6")
    {
        foreach (var s in sensors)
        {
            s.IsOutOfBoundsAttack = false;
            s.IsBadSignatureAttack = false;
            s.IsReplayAttack = false;
            s.IsInactivityAttack = false;
            s.TriggerFloodAttack = false;
        }
        ConsoleManager.WriteLog("[SYSTEM RESTORED] All anomaly parameters mitigated. Sensors reporting standard data streams.", ConsoleColor.Green);
    }
    else if (input == "7")
    {
        Console.Clear();
        Environment.Exit(0);
    }

    ConsoleManager.RefreshInputPrompt();
}

public static class ConsoleManager
{
    private static readonly object _consoleLock = new object();
    private static int _currentLogLine = 14;
    private const int LogStartLine = 14;

    public static void DrawMenuLayout()
    {
        lock (_consoleLock)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================ ATTACK SIMULATION ================================");
            Console.ResetColor();
            Console.WriteLine(" 1. Inject Out-of-Bounds Data Payload  | 5. Trigger DoS Burst Flood Attack");
            Console.WriteLine(" 2. Inject Tampered Cryptic Signatures | 6. Suppress Attacks (Restore Baseline Nodes)");
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
    }

    public static void RefreshInputPrompt()
    {
        lock (_consoleLock)
        {
            Console.SetCursorPosition(0, 8);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, 8);
            Console.Write("Select an option (1-7): ");
        }
    }

    public static void WriteLog(string logMessage, ConsoleColor logColor, ConsoleColor backColor = ConsoleColor.Black)
    {
        lock (_consoleLock)
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
    }
}