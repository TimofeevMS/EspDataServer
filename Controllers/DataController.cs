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
}