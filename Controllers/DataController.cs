using EspDataServer.Data;
using EspDataServer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EspDataServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly AppDbContext _context;

    public DataController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] DataInfo data)
    {
        var sensorDate = new SensorData()
        {
            Temperature = data.Temperature,
            Humidity = data.Humidity,
            Timestamp = DateTime.UtcNow,
        };
        
        _context.SensorReadings.Add(sensorDate);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetSensorData(DateTime? from = null, DateTime? to = null, int? minutes = null,
                                                   CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        IQueryable<SensorData> query = _context.SensorReadings;

        if (minutes.HasValue)
        {
            var since = now.AddMinutes(-minutes.Value);
            query = query.Where(d => d.Timestamp >= since);
        }
        else if (from.HasValue && to.HasValue)
        {
            query = query.Where(d => d.Timestamp >= from && d.Timestamp <= to);
        }

        var data = await query
                        .OrderBy(d => d.Timestamp)
                        .Take(1000)
                        .Select(d => new
                         {
                             timestamp = d.Timestamp,
                             temperature = d.Temperature,
                             humidity = d.Humidity
                         })
                        .ToListAsync(cancellationToken);

        return Ok(data);
    }
}