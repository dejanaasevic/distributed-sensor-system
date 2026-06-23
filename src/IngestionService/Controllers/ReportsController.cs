using IngestionService.Data;
using IngestionService.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetConsensusReports([FromQuery] int limit = 100)
        {
            try
            {
                var dbReports = await _db.ConsensusValues
                    .OrderByDescending(c => c.CalculatedAt)
                    .Take(limit) 
                    .ToListAsync();

                var dtoReports = dbReports.Select(r => new ConsensusReportDto
                {
                    Id = r.Id,
                    CalculatedAt = r.CalculatedAt,
                    Value = r.Value,
                    ParticipatingSensors = !string.IsNullOrEmpty(r.ParticipatingSensors)
                        ? r.ParticipatingSensors.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                        : new List<string>()
                }).ToList();

                return Ok(dtoReports);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving consensus reports: {ex.Message}");
                return StatusCode(500, "An internal server error occurred while processing the reports.");
            }
        }
    }
}