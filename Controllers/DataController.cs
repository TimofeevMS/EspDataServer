using EspDataServer.Data;
using EspDataServer.Models;

using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> Post([FromBody] SensorData data)
    {
        data.Timestamp = DateTime.UtcNow;
        _context.SensorReadings.Add(data);
        await _context.SaveChangesAsync();
        return Ok(new { status = "saved" });
    }
    
    [HttpGet("latest")]
    public IActionResult Latest(int count = 20)
    {
        var data = _context.SensorReadings
                           .OrderByDescending(d => d.Timestamp)
                           .Take(count)
                           .OrderBy(d => d.Timestamp)
                           .ToList();

        return Ok(data);
    }
}