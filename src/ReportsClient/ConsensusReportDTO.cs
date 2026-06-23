namespace ReportsClient
{
    public class ConsensusReportDTO
    {
        public Guid Id { get; set; }
        public DateTime CalculatedAt { get; set; }
        public double Value { get; set; }
        public List<string> ParticipatingSensors { get; set; } = new();
    }
}