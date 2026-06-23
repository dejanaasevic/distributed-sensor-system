using IngestionService.Models;

namespace IngestionService.DTO
{
    public class DecryptedPayloadDTO
    {
        public double Temperature { get; set; }
        public int AlarmPriority { get; set; }
        public DataQuality Quality { get; set; }
    }
}
