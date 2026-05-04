using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PocApp.Models;

namespace PocApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(e =>
        {
            e.HasOne(p => p.Owner)
             .WithMany(u => u.OwnedProjects)
             .HasForeignKey(p => p.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskItem>(e =>
        {
            e.HasOne(t => t.Project)
             .WithMany(p => p.Tasks)
             .HasForeignKey(t => t.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.AssignedUser)
             .WithMany(u => u.AssignedTasks)
             .HasForeignKey(t => t.AssignedUserId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => t.DueDate);
        });
    }
}
