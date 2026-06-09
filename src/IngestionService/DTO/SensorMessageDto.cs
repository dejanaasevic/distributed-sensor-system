using IngestionService.Models;

namespace IngestionService.DTO
{
    public class SensorMessageDto
    {
        public string SensorId { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Signature { get; set; } = string.Empty;
        public string Iv { get; set; } = string.Empty;
        public string Ciphertext { get; set; } = string.Empty;
    }
}
