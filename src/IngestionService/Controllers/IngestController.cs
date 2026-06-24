using IngestionService.Data;
using IngestionService.DTO;
using IngestionService.Models;
using IngestionService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
        private readonly IAlarmNotificationService _alarmNotificationService;
        private readonly IMemoryCache _cache;

        public IngestController(AppDbContext db, IHttpClientFactory httpClientFactory, ISensorSecurityService securityService, ISensorBlockManager blockManager, IAlarmNotificationService alarmNotificationService, IMemoryCache cache)
        {
            _db = db;
            _httpClient = httpClientFactory.CreateClient();
            _securityService = securityService;
            _blockManager = blockManager;
            _alarmNotificationService = alarmNotificationService;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] SensorMessageDto dto)
        {
            Console.WriteLine($"Received message from Sensor {dto.SensorId} with Message ID {dto.MessageId} at {DateTime.UtcNow:HH:mm:ss}");

            // Core DDoS Protection Verification
            if (_blockManager.RecordRequestAndCheckBlock(dto.SensorId))
            {
                return StatusCode(429, $"Sensor {dto.SensorId} is temporarily blocked due to excessive requests (DoS protection).");
            }

            var sensor = await _db.Sensors.FindAsync(dto.SensorId);
            if (sensor == null)
            {
                return BadRequest($"Sensor with ID {dto.SensorId} not found.");
            }

            Console.WriteLine("Passed finding");

            // Replay Attack Control
            if (_securityService.IsReplayAttack(dto.SensorId, dto.MessageId, dto.Timestamp))
            {
                sensor.Quality = DataQuality.BAD;
                await _db.SaveChangesAsync();
                return BadRequest("Security violation: Potential Replay Attack detected.");
            }

            Console.WriteLine("Passed replay attack check");

            // Asymmetric Crypto Verification
            if (!_securityService.VerifySignature(dto, sensor.PublicKey))
            {
                sensor.Quality = DataQuality.BAD;
                await _db.SaveChangesAsync();
                return BadRequest("Security violation: Cryptographic signature verification failed.");
            }

            Console.WriteLine("Passed signature verification");

            DecryptedPayloadDTO? decryptedData = _securityService.DecryptMessage(dto, sensor.SymmetricKey);
            if (decryptedData == null)
            {
                sensor.Quality = DataQuality.BAD;
                await _db.SaveChangesAsync();
                return BadRequest("Security violation: Payload decryption failed.");
            }

            Console.ForegroundColor = decryptedData.AlarmPriority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow,
                3 => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            if (decryptedData.AlarmPriority > 0)
            {
                string formattedString = $"[Alarm] Sensor {dto.SensorId} triggered alarm with priority {decryptedData.AlarmPriority} with Temperature {decryptedData.Temperature} at {DateTime.UtcNow:HH:mm:ss}";
                await _alarmNotificationService.SendNotificationAsync(formattedString, decryptedData.AlarmPriority);
            }

            Console.ResetColor();


            sensor.LastSeenAt = DateTime.UtcNow;
            sensor.IsActive = true;
            sensor.Quality = DataQuality.GOOD;

            SensorReading sensorReading = new SensorReading(dto.SensorId, decryptedData.Temperature, DateTime.UtcNow, sensor.Quality, decryptedData.AlarmPriority, sensor);
            _db.SensorReadings.Add(sensorReading);


            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterSensor([FromBody] SensorRegistrationDto dto)
        {
            var sensor = await _db.Sensors.FindAsync(dto.Id);
            int ordinal;

            var cacheKey = $"last_id_{dto.Id}";

            if (sensor != null)
            {
                Console.WriteLine($"[Update] Sensor with ID {dto.Id} already exists. Changing keys and configuration...");

                sensor.MinTemperature = dto.MinTemperature;
                sensor.MaxTemperature = dto.MaxTemperature;
                sensor.Quality = dto.Quality;
                sensor.AlarmThreshold1 = dto.AlarmThreshold1;
                sensor.AlarmThreshold2 = dto.AlarmThreshold2;
                sensor.AlarmThreshold3 = dto.AlarmThreshold3;

                sensor.PublicKey = dto.PublicKeyXml;
                sensor.SymmetricKey = dto.SymmetricKey;

                await _db.SaveChangesAsync();

                _cache.Remove(cacheKey);

                var allIds = await _db.Sensors.Select(s => s.Id).ToListAsync();
                ordinal = allIds.IndexOf(dto.Id) + 1;
                return Ok(new { Ordinal = ordinal });
            }

            int existingSensorsCount = await _db.Sensors.CountAsync();
            ordinal = existingSensorsCount + 1;

            Console.WriteLine("Registering sensor with id: " + dto.Id);

            sensor = new Sensor(
                dto.Id,
                dto.MinTemperature,
                dto.MaxTemperature,
                dto.Quality,
                dto.AlarmThreshold1,
                dto.AlarmThreshold2,
                dto.AlarmThreshold3,
                dto.PublicKeyXml,
                dto.SymmetricKey
            );

            _db.Sensors.Add(sensor);
            await _db.SaveChangesAsync();

            _cache.Remove(cacheKey);

            return Ok(new { Ordinal = ordinal });
        }

        [HttpPost("block/{sensorId}")]
        public IActionResult BlockSensor(string sensorId)
        {
            _blockManager.BlockSensor(sensorId, 30);
            Console.WriteLine($"[TEST] Sensor {sensorId} manually blocked for 30 seconds.");
            return Ok($"Sensor {sensorId} blocked for 30 seconds.");
        }
    }
}