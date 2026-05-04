using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocApp.Data;
using PocApp.Models;

namespace PocApp.ViewComponents;

public class CalendarViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public CalendarViewComponent(ApplicationDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(int? year = null, int? month = null, int? projectId = null)
    {
        var today = DateTime.Today;
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        var first = new DateTime(y, m, 1);
        var last = first.AddMonths(1).AddDays(-1);

        var query = _db.Tasks.AsNoTracking()
            .Where(t => t.DueDate >= first && t.DueDate <= last);
        if (projectId is int pid)
            query = query.Where(t => t.ProjectId == pid);

        var tasks = await query
            .Include(t => t.Project)
            .Include(t => t.AssignedUser)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        var grouped = tasks
            .GroupBy(t => t.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var vm = new CalendarViewModel
        {
            Year = y,
            Month = m,
            Today = today,
            ProjectId = projectId,
            TasksByDay = grouped
        };
        return View(vm);
    }
}

public class CalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime Today { get; set; }
    public int? ProjectId { get; set; }
    public Dictionary<DateTime, List<TaskItem>> TasksByDay { get; set; } = new();

    public DateTime FirstDay => new(Year, Month, 1);
    public DateTime LastDay => FirstDay.AddMonths(1).AddDays(-1);
    public string MonthLabel => FirstDay.ToString("MMMM yyyy");
    public DateTime PrevMonth => FirstDay.AddMonths(-1);
    public DateTime NextMonth => FirstDay.AddMonths(1);
}
