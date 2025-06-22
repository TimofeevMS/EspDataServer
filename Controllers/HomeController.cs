using System.Diagnostics;

using EspDataServer.Data;

using Microsoft.AspNetCore.Mvc;
using EspDataServer.Models;

using Microsoft.EntityFrameworkCore;

namespace EspDataServer.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var data = await _context.SensorReadings
                                 .OrderByDescending(d => d.Timestamp)
                                 .Take(50)
                                 .ToListAsync(cancellationToken);

        return View(data);
    }

    public async Task<IActionResult> All(CancellationToken cancellationToken = default)
    {
        var data = await _context.SensorReadings
                                 .OrderByDescending(d => d.Timestamp)
                                 .ToListAsync(cancellationToken);

        return View(data);
    }
}