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
    public IActionResult ByDate(DateTime date, int count = 50)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        var data = _context.SensorReadings
                           .Where(d => d.Timestamp >= start && d.Timestamp < end)
                           .OrderByDescending(d => d.Timestamp);

        if (count > 0)
            data = (IOrderedQueryable<SensorData>)data.Take(count);

        var list = data.OrderBy(d => d.Timestamp).ToList();

        return Ok(list);
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