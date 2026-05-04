using Microsoft.EntityFrameworkCore;
using Projects.Api.Models;

namespace Projects.Api.Data;

public class ProjectsDbContext : DbContext
{
    public ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<OwnerView> Owners => Set<OwnerView>();
    public DbSet<TaskRow> TaskRows => Set<TaskRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Project>(e =>
        {
            e.ToTable("Projects");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(150).IsRequired();
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.OwnerId).IsRequired();
            e.HasOne(p => p.Owner)
             .WithMany()
             .HasForeignKey(p => p.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Read-only mapping over the monolith's AspNetUsers table.
        // The microservice should never own this schema — migrations live in PocApp.
        b.Entity<OwnerView>(e =>
        {
            e.ToTable("AspNetUsers");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("Id");
            e.Property(u => u.Email).HasColumnName("Email");
            e.Property(u => u.FullName).HasColumnName("FullName");
        });

        // Read-only mapping over the monolith's Tasks table — used for counting
        // tasks per project AND for the calendar view. Task CRUD stays in the monolith.
        b.Entity<TaskRow>(e =>
        {
            e.ToTable("Tasks");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("Id");
            e.Property(t => t.Title).HasColumnName("Title");
            e.Property(t => t.DueDate).HasColumnName("DueDate");
            e.Property(t => t.Status).HasColumnName("Status");
            e.Property(t => t.ProjectId).HasColumnName("ProjectId");
        });
    }
}

public class TaskRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int Status { get; set; } // 0 Todo, 1 InProgress, 2 Done, 3 Blocked
    public int ProjectId { get; set; }
}
