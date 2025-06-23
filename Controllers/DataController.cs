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
    public async Task<IActionResult> GetAggregatedData(string period = "hour", DateTime? from = null, DateTime? to = null)
    {
        var query = _context.SensorReadings.AsQueryable();

        if (from.HasValue && to.HasValue)
            query = query.Where(d => d.Timestamp >= from && d.Timestamp <= to);

        data;

        switch (period)
        {
            case "day":
                data = await query.GroupBy(d => d.Timestamp.Date)
                                  .Select(g => new
                                   {
                                       timestamp = g.Key,
                                       temperature = g.Average(d => d.Temperature),
                                       humidity = g.Average(d => d.Humidity)
                                   })
                                  .OrderBy(d => d.timestamp)
                                  .ToListAsync();

            break;

            case "week":
                data = await query
                            .GroupBy(d =>
                                         System.Globalization.CultureInfo.InvariantCulture.Calendar
                                               .GetWeekOfYear(d.Timestamp,
                                                              System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                                                              DayOfWeek.Monday))
                            .Select(g => new
                             {
                                 timestamp = $"Неделя {g.Key}",
                                 temperature = g.Average(d => d.Temperature),
                                 humidity = g.Average(d => d.Humidity)
                             })
                            .OrderBy(d => d.timestamp)
                            .ToListAsync();

            break;

            default:
                data = await query
                            .GroupBy(d => new DateTime(d.Timestamp.Year, d.Timestamp.Month, d.Timestamp.Day,
                                                       d.Timestamp.Hour, 0, 0))
                            .Select(g => new
                             {
                                 timestamp = g.Key,
                                 temperature = g.Average(d => d.Temperature),
                                 humidity = g.Average(d => d.Humidity)
                             })
                            .OrderBy(d => d.timestamp)
                            .ToListAsync();

            break;
        }

        return Ok(data);
    }

}