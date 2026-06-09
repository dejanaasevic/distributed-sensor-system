namespace ConsensusService.Models
{
    public class SensorReading
    {
        public Guid Id { get; set; }
        public string SensorId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
        public DataQuality Quality { get; set; }
        public int AlarmPriority { get; set; } = 0;
        public bool IsConsensus { get; set; } = false;
        public Sensor Sensor { get; set; } = null!;
        public SensorReading() { }

        public SensorReading(string sensorId, double temperature, DateTime timestamp, DataQuality quality, int alarmPriority, Sensor sensor)
        {
            Id = Guid.NewGuid();
            SensorId = sensorId;
            Temperature = temperature;
            Timestamp = timestamp;
            Quality = quality;
            AlarmPriority = alarmPriority;
            Sensor = sensor;
        }
    }
}
