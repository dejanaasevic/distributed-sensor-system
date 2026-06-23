using ConsensusService;
using Microsoft.EntityFrameworkCore;
using ConsensusService.Data;
using ConsensusService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IAlarmNotificationService, AlarmNotificationService>();

var host = builder.Build();
host.Run();
