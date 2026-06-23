using AspNetCoreRateLimit;
using IngestionService.Data;
using IngestionService.Services;
using IngestionService.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimit"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<ISensorBlockManager, SensorBlockManager>();
builder.Services.AddSingleton<ISensorSecurityService, SensorSecurityService>();
builder.Services.AddSingleton<IAlarmNotificationService, AlarmNotificationService>();
builder.Services.AddHostedService<SensorTimeoutWorker>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseHttpsRedirection();
// app.UseIpRateLimiting();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();