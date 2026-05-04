using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocApp.Data;
using PocApp.Models;

namespace PocApp.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        ViewBag.Year = year;
        ViewBag.Month = month;
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new UserRowVm
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                ProjectCount = u.OwnedProjects.Count,
                TaskCount = u.AssignedTasks.Count
            })
            .ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> Details(string id, int? year, int? month)
    {
        var user = await _db.Users
            .Include(u => u.OwnedProjects)
            .Include(u => u.AssignedTasks).ThenInclude(t => t.Project)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        ViewBag.Year = year;
        ViewBag.Month = month;
        return View(user);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        return View(new UserEditVm { Id = user.Id, Email = user.Email, FullName = user.FullName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userManager.FindByIdAsync(vm.Id);
        if (user is null) return NotFound();
        user.FullName = vm.FullName;
        if (!string.IsNullOrWhiteSpace(vm.Email) && user.Email != vm.Email)
        {
            user.Email = vm.Email;
            user.UserName = vm.Email;
            user.NormalizedEmail = vm.Email.ToUpperInvariant();
            user.NormalizedUserName = vm.Email.ToUpperInvariant();
        }
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var user = await _db.Users
            .Include(u => u.OwnedProjects)
            .Include(u => u.AssignedTasks)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        return View(user);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return RedirectToAction(nameof(Index));
        var ownsProjects = await _db.Projects.AnyAsync(p => p.OwnerId == id);
        if (ownsProjects)
        {
            ModelState.AddModelError(string.Empty, "Cannot delete a user who owns projects. Reassign the projects first.");
            var fresh = await _db.Users
                .Include(u => u.OwnedProjects)
                .Include(u => u.AssignedTasks)
                .FirstAsync(u => u.Id == id);
            return View("Delete", fresh);
        }
        await _userManager.DeleteAsync(user);
        return RedirectToAction(nameof(Index));
    }
}

public class UserRowVm
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public int ProjectCount { get; set; }
    public int TaskCount { get; set; }
}

public class UserEditVm
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
