using EspDataServer.Models;

using Microsoft.EntityFrameworkCore;

namespace EspDataServer.Data;

public class AppDbContext : DbContext
{
    public DbSet<SensorData> SensorReadings { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}