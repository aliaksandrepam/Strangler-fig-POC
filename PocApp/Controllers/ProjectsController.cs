using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PocApp.Data;
using PocApp.Models;

namespace PocApp.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProjectsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? year, int? month)
    {
        ViewBag.Year = year;
        ViewBag.Month = month;
        var projects = await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(projects);
    }

    public async Task<IActionResult> Details(int id, int? year, int? month)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Tasks).ThenInclude(t => t.AssignedUser)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        ViewBag.Year = year;
        ViewBag.Month = month;
        return View(project);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateOwnersAsync();
        return View(new Project { CreatedAt = DateTime.UtcNow });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,OwnerId,CreatedAt")] Project project)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync(project.OwnerId);
            return View(project);
        }
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return NotFound();
        await PopulateOwnersAsync(project.OwnerId);
        return View(project);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,OwnerId,CreatedAt")] Project project)
    {
        if (id != project.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync(project.OwnerId);
            return View(project);
        }
        _db.Update(project);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        return View(project);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is not null)
        {
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOwnersAsync(string? selected = null)
    {
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, Display = u.FullName ?? u.UserName })
            .ToListAsync();
        ViewBag.Owners = new SelectList(users, "Id", "Display", selected);
    }
}
