using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PocApp.Data;
using PocApp.Models;

namespace PocApp.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly ApplicationDbContext _db;
    public TasksController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? projectId, int? year, int? month)
    {
        var query = _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedUser)
            .AsQueryable();
        if (projectId is int pid) query = query.Where(t => t.ProjectId == pid);

        var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();
        ViewBag.ProjectId = projectId;
        ViewBag.Year = year;
        ViewBag.Month = month;
        ViewBag.Projects = new SelectList(await _db.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", projectId);
        return View(tasks);
    }

    public async Task<IActionResult> Details(int id)
    {
        var task = await _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();
        return View(task);
    }

    public async Task<IActionResult> Create(int? projectId)
    {
        await PopulateLookupsAsync(projectId);
        return View(new TaskItem { ProjectId = projectId ?? 0, DueDate = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,DueDate,Status,ProjectId,AssignedUserId")] TaskItem task)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(task.ProjectId);
            return View(task);
        }
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return NotFound();
        await PopulateLookupsAsync(task.ProjectId, task.AssignedUserId);
        return View(task);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DueDate,Status,ProjectId,AssignedUserId")] TaskItem task)
    {
        if (id != task.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(task.ProjectId, task.AssignedUserId);
            return View(task);
        }
        _db.Update(task);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var task = await _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();
        return View(task);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is not null)
        {
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(int? projectId = null, string? userId = null)
    {
        ViewBag.Projects = new SelectList(await _db.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", projectId);
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, Display = u.FullName ?? u.UserName })
            .ToListAsync();
        ViewBag.Users = new SelectList(users, "Id", "Display", userId);
    }
}
