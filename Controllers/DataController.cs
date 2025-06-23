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
    
    [HttpGet]
    public async Task<IActionResult> GetData(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] int interval = 0,
        CancellationToken cancellationToken = default)
    {
        // Ensure UTC kind
        start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

        // Validate inputs
        if (start >= end)
        {
            return BadRequest("Дата начала должна быть раньше даты конца");
        }
        if (interval is < 0 or > 60)
        {
            return BadRequest("Интервал должен быть от 0 до 60 минут");
        }

        IQueryable<object> query;

        if (interval == 0)
        {
            // Без агрегации: возвращаем все данные
            query = _context.SensorReadings
                .Where(d => d.Timestamp >= start && d.Timestamp <= end)
                .Select(d => new
                {
                    d.Timestamp,
                    d.Temperature,
                    d.Humidity
                })
                .OrderBy(d => d.Timestamp);
        }
        else
        {
            // С агрегацией по интервалу
            query = _context.SensorReadings
                .Where(d => d.Timestamp >= start && d.Timestamp <= end)
                .GroupBy(d => new
                {
                    Timestamp = new DateTime(
                        d.Timestamp.Year,
                        d.Timestamp.Month,
                        d.Timestamp.Day,
                        d.Timestamp.Hour,
                        (d.Timestamp.Minute / interval) * interval,
                        0,
                        DateTimeKind.Utc)
                })
                .Select(g => new
                {
                    Timestamp = g.Key.Timestamp,
                    Temperature = g.Average(x => x.Temperature),
                    Humidity = g.Average(x => x.Humidity)
                })
                .OrderBy(g => g.Timestamp);
        }

        var data = await query.ToListAsync(cancellationToken);
        return Ok(data);
    }
}