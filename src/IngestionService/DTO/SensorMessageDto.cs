using IngestionService.Models;

namespace IngestionService.DTO
{
    public class SensorMessageDto
    {
        public string SensorId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; }
        public int AlarmPriority { get; set; }
        public DataQuality Quality { get; set; }
        public int MessageId { get; set; }
        public string Signature { get; set; } = string.Empty;
    }
}
