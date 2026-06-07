using IngestionService.Data;
using IngestionService.DTO;
using IngestionService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IngestionService.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class IngestController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;

        public IngestController(AppDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] SensorMessageDto dto)
        {
            var sensor = await _db.Sensors.FindAsync(dto.SensorId);
            if (sensor == null)
            {
                return BadRequest($"Sensor with {dto.SensorId} not found");
            }

            sensor.LastSeenAt = DateTime.UtcNow;
            sensor.IsActive = true;

            SensorReading sensorReading = new SensorReading(dto.SensorId, dto.Temperature, DateTime.UtcNow, dto.Quality, dto.AlarmPriority, sensor);
            _db.SensorReadings.Add(sensorReading);
            if (dto.AlarmPriority > 0)
            {
                await _httpClient.PostAsJsonAsync("http://notification:5002/api/notify", dto);
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterSensor([FromBody] SensorRegistrationDto dto)
        {
            var sensor = await _db.Sensors.FindAsync(dto.Id);
            if(sensor != null)
            {
                return BadRequest($"Sensor with {dto.Id} already exist");

            }
            sensor = new Sensor(dto.Id, dto.MinTemperature, dto.MaxTemperature, dto.Quality, dto.AlarmThreshold1, dto.AlarmThreshold2, dto.AlarmThreshold3);
            _db.Sensors.Add(sensor);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}