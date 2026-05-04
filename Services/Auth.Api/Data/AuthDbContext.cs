using Auth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Data;

/// <summary>
/// Read-only access to AspNetUsers. The microservice does NOT own the schema —
/// migrations live in the MVC monolith. We map only the columns we need.
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("AspNetUsers");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("Id");
            e.Property(u => u.UserName).HasColumnName("UserName");
            e.Property(u => u.NormalizedUserName).HasColumnName("NormalizedUserName");
            e.Property(u => u.Email).HasColumnName("Email");
            e.Property(u => u.NormalizedEmail).HasColumnName("NormalizedEmail");
            e.Property(u => u.PasswordHash).HasColumnName("PasswordHash");
            e.Property(u => u.FullName).HasColumnName("FullName");
            e.Property(u => u.EmailConfirmed).HasColumnName("EmailConfirmed");
            e.Property(u => u.LockoutEnabled).HasColumnName("LockoutEnabled");
            e.Property(u => u.LockoutEnd).HasColumnName("LockoutEnd");
            e.Property(u => u.AccessFailedCount).HasColumnName("AccessFailedCount");

            e.HasIndex(u => u.NormalizedEmail);
        });
    }
}
