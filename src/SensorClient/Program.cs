using SensorClient;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading;
using System.Threading.Tasks;

var httpClient = new HttpClient();
string baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost";
string serverUrl;
string hubUrl;

if (baseUrl == "http://localhost")
{
    // Docker-compose default fallback: different ports
    serverUrl = "http://localhost:5001/api/ingest";
    hubUrl = "http://localhost:5002/notificationHub";
}
else
{
    // Standard URL (Ingress or custom base)
    serverUrl = $"{baseUrl}/api/ingest";
    hubUrl = $"{baseUrl}/notificationHub";
}

Random random = new Random();
var sensors = new List<Sensor>();

for (int i = 1; i <= 10; i++)
{
    string id = $"SENSOR-{i}";
    double minTemperature = random.NextDouble() * 40.0 + 20.0; // Realistic values between 20°C and 60°C
    double maxTemperature = random.NextDouble() * (100.0 - minTemperature) + minTemperature;
    double alarmThreshold1 = random.NextDouble() * (maxTemperature - minTemperature) + minTemperature;
    double alarmThreshold2 = random.NextDouble() * (maxTemperature - alarmThreshold1) + alarmThreshold1;
    double alarmThreshold3 = random.NextDouble() * (maxTemperature - alarmThreshold2) + alarmThreshold2;

    SensorConfig config = new SensorConfig(minTemperature, maxTemperature, alarmThreshold1, alarmThreshold2, alarmThreshold3, DataQuality.GOOD);
    config.Id = id;
    config.FriendlyName = id;
    Sensor sensor = new Sensor(config);
    sensors.Add(sensor);
}

Console.Clear();
Console.WriteLine("=== Registering 10 Sensors with Server ===");
foreach (var sensor in sensors)
{
    try
    {
        var response = await httpClient.PostAsJsonAsync($"{serverUrl}/register", sensor.Config);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"{sensor.Config.Id} registered with secure keys successfully.");
        }
        else
        {
            Console.WriteLine($"{sensor.Config.Id} registration returned: {response.StatusCode}. Already registered?");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{sensor.Config.Id} registration failed: {ex.Message}");
    }
}

Console.WriteLine("\nInitializing standalone security dashboard...");
Thread.Sleep(1500);

ConsoleManager.DrawMenuLayout();

// Start first 5 active sensors
var cancellationTokens = new Dictionary<string, CancellationTokenSource>();
var runningTasks = new Dictionary<string, Task>();

for (int i = 0; i < 5; i++)
{
    var sensor = sensors[i];
    var cts = new CancellationTokenSource();
    cancellationTokens[sensor.Config.Id] = cts;
    sensor.IsRunning = true;
    runningTasks[sensor.Config.Id] = sensor.RunAsync(httpClient, serverUrl, cts.Token);
    ConsoleManager.WriteLog($"[INIT] Started active sensor: {sensor.Config.FriendlyName}", ConsoleColor.Green);
}

// Connect to NotificationService SignalR Hub
var hubConnection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

hubConnection.On<string>("SensorInactive", async (sensorId) =>
{
    var sensorToDeactivate = sensors.FirstOrDefault(s => s.Config.Id == sensorId);
    if (sensorToDeactivate != null && sensorToDeactivate.IsRunning)
    {
        ConsoleManager.WriteLog($"[SIGNALR] Server detected {sensorId} as INACTIVE.", ConsoleColor.Red, ConsoleColor.DarkRed);

        // Deactivate inactive sensor
        if (cancellationTokens.TryGetValue(sensorId, out var cts))
        {
            cts.Cancel();
            cancellationTokens.Remove(sensorId);
        }
        sensorToDeactivate.IsRunning = false;
        if (runningTasks.TryGetValue(sensorId, out var task))
        {
            try { await task; } catch { }
            runningTasks.Remove(sensorId);
        }
        ConsoleManager.WriteLog($"[CLEANUP] Stopped thread loop for inactive {sensorId}.", ConsoleColor.DarkYellow);

        // Find and activate a standby sensor
        var standbySensor = sensors.FirstOrDefault(s => !s.IsRunning && s.Config.Id != sensorId);
        if (standbySensor != null)
        {
            ConsoleManager.WriteLog($"[STANDBY] Activating standby replacement sensor {standbySensor.Config.FriendlyName}...", ConsoleColor.Green);
            var newCts = new CancellationTokenSource();
            cancellationTokens[standbySensor.Config.Id] = newCts;
            standbySensor.IsRunning = true;
            runningTasks[standbySensor.Config.Id] = standbySensor.RunAsync(httpClient, serverUrl, newCts.Token);
        }
        else
        {
            ConsoleManager.WriteLog("[WARNING] No more standby sensors available in pool!", ConsoleColor.Yellow);
        }
    }
});

try
{
    await hubConnection.StartAsync();
    ConsoleManager.WriteLog("[SIGNALR] Connected to Notification Hub. Standby Manager Active.", ConsoleColor.Green);
}
catch (Exception ex)
{
    ConsoleManager.WriteLog($"[SIGNALR] Connection failed: {ex.Message}", ConsoleColor.Red);
}

while (true)
{
    Console.SetCursorPosition(24, 8);
    string input = Console.ReadLine();
    var activeSensors = sensors.Where(s => s.IsRunning).ToList();

    if (input == "1")
    {
        if (activeSensors.Count >= 1)
        {
            activeSensors[0].IsOutOfBoundsAttack = true;
            ConsoleManager.WriteLog($"[ATTACK ENGAGED] {activeSensors[0].Config.FriendlyName} set to inject Out-of-Bounds metric.", ConsoleColor.Magenta);
        }
    }
    else if (input == "2")
    {
        if (activeSensors.Count >= 2)
        {
            activeSensors[1].IsBadSignatureAttack = true;
            ConsoleManager.WriteLog($"[ATTACK ENGAGED] {activeSensors[1].Config.FriendlyName} set to transmit Malformed Crypto Signatures.", ConsoleColor.Magenta);
        }
    }
    else if (input == "3")
    {
        if (activeSensors.Count >= 3)
        {
            activeSensors[2].IsReplayAttack = true;
            ConsoleManager.WriteLog($"[ATTACK ENGAGED] {activeSensors[2].Config.FriendlyName} set to loop Stale sequence ID messages.", ConsoleColor.Magenta);
        }
    }
    else if (input == "4")
    {
        if (activeSensors.Count >= 4)
        {
            activeSensors[3].IsInactivityAttack = true;
            ConsoleManager.WriteLog($"[ATTACK ENGAGED] {activeSensors[3].Config.FriendlyName} silenced. Simulating critical node failure drop.", ConsoleColor.Magenta);
        }
    }
    else if (input == "5")
    {
        if (activeSensors.Count >= 5)
        {
            activeSensors[4].TriggerFloodAttack = true;
            ConsoleManager.WriteLog($"[ATTACK ENGAGED] {activeSensors[4].Config.FriendlyName} launching massive burst-rate DoS flood.", ConsoleColor.Magenta);
        }
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
        foreach (var cts in cancellationTokens.Values)
        {
            cts.Cancel();
        }
        try { await hubConnection.DisposeAsync(); } catch { }
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
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine("=== Standalone Sensor Client Simulation Menu ===");
            return;
        }
        lock (_consoleLock)
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================ ATTACK SIMULATION ================================");
            Console.ResetColor();
            Console.WriteLine(" 1. Inject Out-of-Bounds Data (Active 1) | 5. Trigger DoS Burst Flood (Active 5)");
            Console.WriteLine(" 2. Inject Tampered Signatures (Active 2)| 6. Suppress Attacks (Restore Baseline)");
            Console.WriteLine(" 3. Inject Replay Stale IDs (Active 3)   | 7. Terminate Process Application");
            Console.WriteLine(" 4. Trigger Inactivity Dropout (Active 4)|");
            Console.WriteLine("-------------------------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" Notice: Attack targets maps dynamically to current active sensors.");
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
        if (Console.IsOutputRedirected) return;
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
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine(logMessage);
            Console.Out.Flush();
            return;
        }
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