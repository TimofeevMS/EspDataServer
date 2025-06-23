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
    [HttpGet]
    public async Task<IActionResult> GetData([FromQuery] DateTime start,
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

        // Restrict range to prevent overloading
        if ((end - start).TotalDays > 7)
        {
            return BadRequest("Диапазон не должен превышать 7 дней");
        }

        if (interval == 0)
        {
            // Без агрегации: возвращаем все данные
            var data = await _context.SensorReadings
                .Where(d => d.Timestamp >= start && d.Timestamp <= end)
                .Select(d => new
                {
                    d.Timestamp,
                    d.Temperature,
                    d.Humidity
                })
                .OrderBy(d => d.Timestamp)
                .Take(10000) // Ограничение на 10,000 записей для защиты
                .ToListAsync(cancellationToken);

            return Ok(data);
        }
        else
        {
            // Агрегация по интервалу с использованием raw SQL
            var query = @"
                SELECT 
                    strftime('%Y-%m-%d %H:%M:00', Timestamp, 'start of minute', 
                             '-' || (strftime('%M', Timestamp) % @p0) || ' minutes') AS Timestamp,
                    AVG(Temperature) AS Temperature,
                    AVG(Humidity) AS Humidity
                FROM SensorReadings
                WHERE Timestamp >= @p1 AND Timestamp <= @p2
                GROUP BY 
                    strftime('%Y-%m-%d %H:%M:00', Timestamp, 'start of minute', 
                             '-' || (strftime('%M', Timestamp) % @p0) || ' minutes')
                ORDER BY Timestamp";

            var data = await _context.Database
                .SqlQueryRaw<SensorData>(query, interval, start, end)
                .ToListAsync(cancellationToken);

            return Ok(data);
        }
    }
}