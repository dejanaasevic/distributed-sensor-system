using ConsensusService.Data;
using ConsensusService.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsensusService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string SystemConsensusId = "SYSTEM-CONSENSUS";
        private HubConnection _connection;
        private string _hubUrl;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            // read url from configuration
            _hubUrl = _configuration["NotificationHubUrl"] ?? "http://notification:8080/notificationHub";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConsensusService Worker started.");

            // connect to notification hub
            _connection = new HubConnectionBuilder().WithUrl(_hubUrl).WithAutomaticReconnect().Build();
            
            try { 
                await _connection.StartAsync(stoppingToken);
            }

            catch (Exception ex) {
                _logger.LogWarning("Could not connect to NotificationHub: {msg}", ex.Message); 
            }

            // Align execution to run every minute
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait until the next minute boundary (e.g. at second 0)
                    var now = DateTime.UtcNow;
                    var delayMs = (60 - now.Second) * 1000 - now.Millisecond;
                    if (delayMs <= 0) delayMs = 60000;
                    
                    _logger.LogInformation("Waiting {seconds}s for the next minute consensus calculation window...", Math.Round(delayMs / 1000.0, 1));
                    await Task.Delay(delayMs, stoppingToken);

                    await CalculateConsensusAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in ConsensusService loop.");
                    // In case of error, wait 5 seconds before retrying
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task CalculateConsensusAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var end = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
            var start = end.AddMinutes(-1);

            _logger.LogInformation("Consensus calculation window: {start:HH:mm:ss} to {end:HH:mm:ss}", start, end);

            // Ensure the system consensus sensor exists
            var systemSensor = await db.Sensors.FindAsync(SystemConsensusId);
            if (systemSensor == null)
            {
                systemSensor = new Sensor(SystemConsensusId, 0, 1000, DataQuality.GOOD, 0, 0, 0, "");
                db.Sensors.Add(systemSensor);
                await db.SaveChangesAsync();
            }

            // Fetch all sensors that are active
            var sensors = await db.Sensors
                .Where(s => s.Id != SystemConsensusId)
                .ToListAsync(stoppingToken);

            // 1. Detect Malicious Sensors (stopped responding / lagging)
            // If an active sensor has sent no readings in the last 60 seconds, it's marked as BAD.
            var activeSensorIdsWithReadings = await db.SensorReadings
                .Where(r => r.Timestamp >= start && r.Timestamp <= end && !r.IsConsensus && r.SensorId != SystemConsensusId)
                .Select(r => r.SensorId)
                .Distinct()
                .ToListAsync(stoppingToken);

            foreach (var sensor in sensors)
            {
                if (sensor.IsActive)
                {
                    if (!activeSensorIdsWithReadings.Contains(sensor.Id))
                    {
                        _logger.LogWarning("Sensor {sensorId} sent no readings in the last minute. Marking as MALICIOUS (Quality = BAD).", sensor.Id);
                        sensor.Quality = DataQuality.BAD;
                    }
                    else if (sensor.Quality == DataQuality.BAD)
                    {
                        // recover sensor
                        _logger.LogInformation("Sensor {sensorId} resumed sending readings. Restoring Quality to GOOD.", sensor.Id);
                        sensor.Quality = DataQuality.GOOD;
                    }
                }
            }
            await db.SaveChangesAsync(stoppingToken);

            // Fetch all raw readings (IsConsensus = false) in the previous minute
            var readings = await db.SensorReadings
                .Include(r => r.Sensor)
                .Where(r => r.Timestamp >= start && r.Timestamp <= end && !r.IsConsensus && r.SensorId != SystemConsensusId)
                .ToListAsync(stoppingToken);

            // Filter readings to only include sensors with GOOD quality
            var goodReadings = readings
                .Where(r => r.Sensor.Quality == DataQuality.GOOD)
                .ToList();

            if (!goodReadings.Any())
            {
                _logger.LogWarning("No sensor readings of quality GOOD were found in the last minute. Consensus cannot be calculated.");
                return;
            }

            // Group by SensorId and calculate average proposed value per sensor
            var sensorProposals = goodReadings
                .GroupBy(r => r.SensorId)
                .Select(g => new { SensorId = g.Key, ProposedValue = g.Average(r => r.Temperature) })
                .ToList();

            int N = sensorProposals.Count;
            var proposalsList = sensorProposals.Select(p => p.ProposedValue).OrderBy(v => v).ToList();
            
            // 2. Apply Byzantine Fault Tolerance (BFT) algorithm on the sensor proposals
            // Max tolerable malicious nodes: f = (N - 1) / 3
            int f = (N - 1) / 3;
            double consensusValue = 0;

            _logger.LogInformation("Number of participating nodes (N) = {N}. Tolerable Byzantine nodes (f) = {f}", N, f);
            _logger.LogInformation("Sensor proposals: {proposals}", string.Join(", ", proposalsList.Select(v => $"{Math.Round(v, 2)}°C")));

            if (f > 0 && N > 2 * f)
            {
                // Discard f lowest and f highest values
                var bftProposals = proposalsList.Skip(f).Take(N - 2 * f).ToList();
                consensusValue = bftProposals.Average();
                _logger.LogInformation("BFT applied. Discarded {f} extreme values from each end. Remaining: {bftProposals}. Consensus: {value}°C", f, string.Join(", ", bftProposals.Select(v => $"{Math.Round(v, 2)}°C")), Math.Round(consensusValue, 2));
            }
            else
            {
                // If f == 0 or not enough nodes to discard, take the simple average of all proposals
                consensusValue = proposalsList.Average();
                _logger.LogInformation("BFT not applicable or f = 0. Using simple average of all proposals. Consensus: {value}°C", Math.Round(consensusValue, 2));
            }

            string participatingSensorsStr = string.Join(",", sensorProposals.Select(p => p.SensorId));

            // 3. Persist the Consensus Value to ConsensusValues table (special table for CQRS)
            ConsensusValue cvRecord = new ConsensusValue
            {
                Id = Guid.NewGuid(),
                CalculatedAt = DateTime.UtcNow,
                Value = consensusValue,
                ParticipatingSensors = participatingSensorsStr
            };
            db.ConsensusValues.Add(cvRecord);

            // 4. Persist the Consensus Value to SensorReadings table with IsConsensus flag
            SensorReading consensusReading = new SensorReading(
                SystemConsensusId,
                consensusValue,
                DateTime.UtcNow,
                DataQuality.GOOD,
                0,
                systemSensor
            )
            {
                IsConsensus = true
            };
            db.SensorReadings.Add(consensusReading);

            await db.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Successfully saved consensus value {value}°C to the database.", Math.Round(consensusValue, 2));

            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendNotification", $"[CONSENSUS] New consensus value: {Math.Round(consensusValue, 2)}°C at {DateTime.UtcNow:HH:mm:ss}");
            }
        }
    }
}
