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
        return Ok(new { status = "saved" });
    }
    
    [HttpGet("latest")]
    public async Task<IActionResult> Latest(int count = 20, CancellationToken cancellationToken = default)
    {
        var data = await _context.SensorReadings
                           .OrderByDescending(d => d.Timestamp)
                           .Take(count)
                           .OrderBy(d => d.Timestamp)
                           .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("bydate")]
    public async Task<IActionResult> ByDate([FromQuery] DateTime date,
                                            [FromQuery] int interval = 5,
                                            CancellationToken cancellationToken = default)
    {
        date = date.Date;

        var endDate = date.AddDays(1);

        var rawData = await _context.SensorReadings
                                    .Where(d => d.Timestamp >= date && d.Timestamp < endDate)
                                    .OrderBy(d => d.Timestamp)
                                    .ToListAsync(cancellationToken);

        var data = rawData
                  .GroupBy(d => new
                   {
                       Timestamp = DateTime.SpecifyKind(
                                                        new DateTime(d.Timestamp.Year,
                                                                     d.Timestamp.Month,
                                                                     d.Timestamp.Day,
                                                                     d.Timestamp.Hour,
                                                                     d.Timestamp.Minute - (d.Timestamp.Minute % interval),
                                                                     0),
                                                        DateTimeKind.Utc),
                   })
                  .Select(g => new
                   {
                       Timestamp = g.Key.Timestamp,
                       Temperature = g.Average(x => x.Temperature),
                       Humidity = g.Average(x => x.Humidity)
                   })
                  .OrderBy(g => g.Timestamp)
                  .ToList();

        return Ok(data);
    }

    [HttpGet("byrange")]
    public async Task<IActionResult> ByRange([FromQuery] DateTime startDateTime,
                                             [FromQuery] DateTime endDateTime,
                                             CancellationToken cancellationToken = default)
    {
        startDateTime = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc);
        endDateTime = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc);

        if (startDateTime >= endDateTime)
        {
            return BadRequest("End date must be after start date");
        }

        var data = await _context.SensorReadings
                                 .Where(d => d.Timestamp >= startDateTime && d.Timestamp <= endDateTime)
                                 .OrderBy(d => d.Timestamp)
                                 .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("all")]
    public async Task<IActionResult> All()
    {
        var data = await _context.SensorReadings.ToListAsync();
        return Ok(data);
    }
    
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        _context.SensorReadings.RemoveRange(_context.SensorReadings);
        await _context.SaveChangesAsync();
        return Ok();
    }
}