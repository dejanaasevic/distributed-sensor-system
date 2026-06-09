using IngestionService.Data;
using IngestionService.DTO;
using IngestionService.Models;
using IngestionService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IngestionService.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    
    public class IngestController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly ISensorSecurityService _securityService;
        private readonly ISensorBlockManager _blockManager;

        public IngestController(AppDbContext db, IHttpClientFactory httpClientFactory, ISensorSecurityService securityService, ISensorBlockManager blockManager)
        {
            _db = db;
            _httpClient = httpClientFactory.CreateClient();
            _securityService = securityService;
            _blockManager = blockManager;
        }

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] SensorMessageDto dto)
        {
            Console.ForegroundColor = dto.AlarmPriority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow,
                3 => ConsoleColor.Red,
                _ => ConsoleColor.White // Handles 0 and any unexpected numbers (default)
            };

            string formattedString = $"Received message from Sensor {dto.SensorId} with Temperature {dto.Temperature} at {dto.Timestamp} and AlarmPriority {dto.AlarmPriority}";
            Console.WriteLine(formattedString);

            // Resets the console to the system default instead of forcing hardcoded White
            Console.ResetColor();

            if (_blockManager.IsBlocked(dto.SensorId))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, $"Sensor {dto.SensorId} is blocked due to spamming.");
            }

            if (_blockManager.RecordRequestAndCheckBlock(dto.SensorId))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[SECURITY] Sensor {dto.SensorId} blocked for 30 seconds due to rate limit violation (>10 msg/sec).");
                Console.ResetColor();
                return StatusCode(StatusCodes.Status429TooManyRequests, $"Sensor {dto.SensorId} blocked for 30 seconds due to spamming.");
            }


            var sensor = await _db.Sensors.FindAsync(dto.SensorId);
            if (sensor == null)
            {
                return BadRequest($"Sensor with {dto.SensorId} not found");
            }


            // Rate Limiting Validation
            if (_securityService.IsRateLimited(dto.SensorId))
            {
                sensor.Quality = DataQuality.BAD;
                await _db.SaveChangesAsync();
                return StatusCode(429, "Rate limit exceeded.");
            }

            // Replay Attack Validation)
            if (_securityService.IsReplayAttack(dto.SensorId, dto.MessageId, dto.Timestamp))
            {
                sensor.Quality = DataQuality.BAD;
                await _db.SaveChangesAsync();
                return BadRequest("Security violation: Potential Replay Attack detected.");
            }


            // Cryptographic Signature Verification
            // if (!_securityService.VerifySignature(dto, sensor.PublicKey))
            // {
            //     sensor.Quality = DataQuality.BAD;
            //     await _db.SaveChangesAsync();
            //     return BadRequest("Security violation: Cryptographic signature verification failed.");
            // }


            // Value Range Validation
            if (dto.Temperature < sensor.MinTemperature || dto.Temperature > sensor.MaxTemperature)
            {
                sensor.Quality = DataQuality.BAD;
                await _db.SaveChangesAsync();
                return BadRequest("Malicious behavior: Data value out of realistic sensor bounds.");
            }


            // success path: update metadata and record clean readings
            sensor.LastSeenAt = DateTime.UtcNow;
            sensor.IsActive = true;
            sensor.Quality = DataQuality.GOOD;


            SensorReading sensorReading = new SensorReading(dto.SensorId, dto.Temperature, DateTime.UtcNow, sensor.Quality, dto.AlarmPriority, sensor);
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
            if (sensor != null)
            {
                return BadRequest($"Sensor with {dto.Id} already exists");
            }

            sensor = new Sensor(dto.Id, dto.MinTemperature, dto.MaxTemperature, dto.Quality, dto.AlarmThreshold1, dto.AlarmThreshold2, dto.AlarmThreshold3, dto.PublicKey);

            _db.Sensors.Add(sensor);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("block/{sensorId}")]
        public IActionResult BlockSensor(string sensorId)
        {
            _blockManager.BlockSensor(sensorId, 30);
            Console.WriteLine($"[Test] Sensor {sensorId} manually blocked for 30 seconds.");
            return Ok($"Sensor {sensorId} blocked for 30 seconds.");
        }
    }
}