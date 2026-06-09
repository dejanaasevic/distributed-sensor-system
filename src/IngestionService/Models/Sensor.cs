namespace IngestionService.Models
{
    public class Sensor
    {
        public string Id { get; set; } = string.Empty;
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public DataQuality Quality { get; set; } = DataQuality.GOOD;
        public bool IsActive { get; set; } = false;
        public DateTime? LastSeenAt { get; set; }
        public double AlarmThreshold1 { get; set; }
        public double AlarmThreshold2 { get; set; }
        public double AlarmThreshold3 { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string SymmetricKey { get; set; } = string.Empty;

        public ICollection<SensorReading> Readings { get; set; } = new List<SensorReading>();

        public Sensor(string id, double minTemperature, double maxTemperature, DataQuality quality, double alarmThreshold1, double alarmThreshold2, double alarmThreshold3, string publicKey = "", string symmetricKey = "")
        {
            Id = id;
            MinTemperature = minTemperature;
            MaxTemperature = maxTemperature;
            Quality = quality;
            AlarmThreshold1 = alarmThreshold1;
            AlarmThreshold2 = alarmThreshold2;
            AlarmThreshold3 = alarmThreshold3;
            PublicKey = publicKey;
            SymmetricKey = symmetricKey;
        }
    }
}
