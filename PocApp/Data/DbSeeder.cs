using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PocApp.Models;
using TaskStatus = PocApp.Models.TaskStatus;

namespace PocApp.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync()) return;

        var alice = new ApplicationUser
        {
            UserName = "alice@example.com",
            Email = "alice@example.com",
            EmailConfirmed = true,
            FullName = "Alice Anderson"
        };
        var bob = new ApplicationUser
        {
            UserName = "bob@example.com",
            Email = "bob@example.com",
            EmailConfirmed = true,
            FullName = "Bob Brown"
        };

        await userManager.CreateAsync(alice, "Passw0rd!");
        await userManager.CreateAsync(bob, "Passw0rd!");

        var apollo = new Project
        {
            Name = "Apollo Migration",
            Description = "Move legacy services to the new platform.",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            OwnerId = alice.Id
        };
        var orion = new Project
        {
            Name = "Orion Reporting",
            Description = "Build the new analytics dashboard.",
            CreatedAt = DateTime.UtcNow.AddDays(-12),
            OwnerId = bob.Id
        };
        db.Projects.AddRange(apollo, orion);
        await db.SaveChangesAsync();

        var today = DateTime.Today;
        db.Tasks.AddRange(
            new TaskItem { Title = "Inventory legacy endpoints", DueDate = today.AddDays(-3), Status = TaskStatus.Done, ProjectId = apollo.Id, AssignedUserId = alice.Id },
            new TaskItem { Title = "Draft migration plan", DueDate = today.AddDays(1), Status = TaskStatus.InProgress, ProjectId = apollo.Id, AssignedUserId = alice.Id },
            new TaskItem { Title = "Set up staging environment", DueDate = today.AddDays(3), Status = TaskStatus.Todo, ProjectId = apollo.Id, AssignedUserId = bob.Id },
            new TaskItem { Title = "Cutover dry-run", DueDate = today.AddDays(10), Status = TaskStatus.Todo, ProjectId = apollo.Id, AssignedUserId = alice.Id },
            new TaskItem { Title = "Define metrics catalog", DueDate = today, Status = TaskStatus.InProgress, ProjectId = orion.Id, AssignedUserId = bob.Id },
            new TaskItem { Title = "Wire up data pipeline", DueDate = today.AddDays(5), Status = TaskStatus.Todo, ProjectId = orion.Id, AssignedUserId = bob.Id },
            new TaskItem { Title = "Stakeholder review", DueDate = today.AddDays(14), Status = TaskStatus.Todo, ProjectId = orion.Id, AssignedUserId = alice.Id }
        );
        await db.SaveChangesAsync();
    }
}
