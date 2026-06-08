using IngestionService.Models;

namespace IngestionService.DTO
{
    public class SensorRegistrationDto
    {
        public string Id { get; set; } = string.Empty;
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public DataQuality Quality { get; set; } = DataQuality.GOOD;
        public double AlarmThreshold1 { get; set; }
        public double AlarmThreshold2 { get; set; }
        public double AlarmThreshold3 { get; set; }
        public string PublicKey { get; set; } = string.Empty;
    }
}