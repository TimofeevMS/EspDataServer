using System.Diagnostics;

using EspDataServer.Data;

using Microsoft.AspNetCore.Mvc;
using EspDataServer.Models;

namespace EspDataServer.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var data = _context.SensorReadings
                           .OrderByDescending(d => d.Timestamp)
                           .Take(50)
                           .ToList();

        return View(data);
    }
}
