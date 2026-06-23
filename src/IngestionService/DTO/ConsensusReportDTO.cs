namespace IngestionService.DTO
{
    public class ConsensusReportDto
    {
        public Guid Id { get; set; }
        public DateTime CalculatedAt { get; set; }
        public double Value { get; set; }
        public List<string> ParticipatingSensors { get; set; } = new();
    }
}