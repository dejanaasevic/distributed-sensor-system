using IngestionService.Data;
using IngestionService.Models;
using Microsoft.EntityFrameworkCore;

namespace IngestionService.Workers;

public class SensorTimeoutWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SensorTimeoutWorker> _logger;

    public SensorTimeoutWorker(IServiceProvider serviceProvider, ILogger<SensorTimeoutWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var timeoutThreshold = DateTime.UtcNow.AddSeconds(-10);

                // active, GOOD sensors that missed check for more than 10s
                var inactiveSensors = await db.Sensors
                    .Where(s => s.IsActive && s.Quality == DataQuality.GOOD && s.LastSeenAt < timeoutThreshold)
                    .ToListAsync(stoppingToken);

                if (inactiveSensors.Any())
                {
                    foreach (var sensor in inactiveSensors)
                    {
                        sensor.Quality = DataQuality.BAD;
                        sensor.IsActive = false;
                        _logger.LogWarning("Timeout anomaly: Sensor {Id} missed its 10s window. Flipped to BAD.", sensor.Id);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}