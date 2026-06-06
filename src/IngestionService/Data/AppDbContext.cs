using IngestionService.Models;
using Microsoft.EntityFrameworkCore;

namespace IngestionService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // define tables based on models
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<ConsensusValue> ConsensusValues { get; set; }
        public DbSet<SensorReading> SensorReadings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // configure primary keys
            modelBuilder.Entity<Sensor>().HasKey(s => s.Id);
            modelBuilder.Entity<ConsensusValue>().HasKey(cv => cv.Id);
            modelBuilder.Entity<SensorReading>().HasKey(sr => sr.Id);

            // configure the relationship between tables
            modelBuilder.Entity<SensorReading>().HasOne(sr => sr.Sensor).WithMany(s => s.Readings).HasForeignKey(sr => sr.SensorId);
        }
    }
}