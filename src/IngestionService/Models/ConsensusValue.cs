
namespace IngestionService.Models
{
    public class ConsensusValue
    {
        public Guid Id { get; set; }
        public DateTime CalculatedAt { get; set; }
        public double Value { get; set; }
        public string ParticipatingSensors { get; set; } = string.Empty;
    }

}
