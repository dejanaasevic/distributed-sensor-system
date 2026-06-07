using SensorClient;

var httpClient = new HttpClient();
var serverUrl = "http://localhost:5001/api/ingest";

Random random = new Random();
var sensors = new List<Sensor>();

for(int i = 1; i <=5; i++)
{
    string id = $"SENSOR-{i}";
    double minTemperature = random.NextDouble() * 40.0 + 280.0;
    double maxTemperature = random.NextDouble() * (320.0 - minTemperature) + minTemperature;
    double alarmThreshold1 = random.NextDouble() * (maxTemperature - minTemperature) + minTemperature;
    double alarmThreshold2 = random.NextDouble() * (maxTemperature - alarmThreshold1) + alarmThreshold1;
    double alarmThreshold3 = random.NextDouble() * (maxTemperature - alarmThreshold2) + alarmThreshold2;

    SensorConfig config = new SensorConfig(id, minTemperature, maxTemperature, alarmThreshold1, alarmThreshold2, alarmThreshold3, DataQuality.GOOD);
    Sensor sensor = new Sensor(config);
    sensors.Add(sensor);
}

var tasks = sensors.Select(s => s.RunAsync(httpClient, serverUrl));
await Task.WhenAll(tasks);