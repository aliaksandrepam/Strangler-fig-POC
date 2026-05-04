using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocApp.Data;
using PocApp.Models;
using TaskStatus = PocApp.Models.TaskStatus;

namespace PocApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [Authorize]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        ViewBag.Year = year;
        ViewBag.Month = month;
        ViewBag.UserCount = await _db.Users.CountAsync();
        ViewBag.ProjectCount = await _db.Projects.CountAsync();
        ViewBag.OpenTaskCount = await _db.Tasks.CountAsync(t => t.Status != TaskStatus.Done);
        ViewBag.UpcomingTasks = await _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedUser)
            .Where(t => t.Status != TaskStatus.Done && t.DueDate >= DateTime.Today)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .ToListAsync();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
