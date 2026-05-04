using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Projects.Api.Contracts;
using Projects.Api.Data;
using Projects.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// ----- DB: shares the monolith's SQLite file. Microservice does NOT own the schema. -----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ProjectsDbContext>(o => o.UseSqlite(connectionString));

// ----- JWT bearer authentication (key/issuer/audience shared with the monolith) -----
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// CORS — only used if the SPA hits the API directly (bypassing the gateway).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:5173", "http://localhost:8080")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ===== Endpoints =====
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "Projects.Api" }));

var projects = app.MapGroup("/api/projects").RequireAuthorization();

projects.MapGet("/", async (ProjectsDbContext db) =>
{
    var list = await db.Projects
        .AsNoTracking()
        .Include(p => p.Owner)
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new ProjectDto(
            p.Id,
            p.Name,
            p.Description,
            p.CreatedAt,
            p.OwnerId,
            p.Owner != null ? p.Owner.FullName : null,
            p.Owner != null ? p.Owner.Email : null,
            db.TaskRows.Count(t => t.ProjectId == p.Id)))
        .ToListAsync();
    return Results.Ok(list);
});

projects.MapGet("/{id:int}", async (int id, ProjectsDbContext db) =>
{
    var p = await db.Projects.AsNoTracking()
        .Include(x => x.Owner)
        .FirstOrDefaultAsync(x => x.Id == id);
    if (p is null) return Results.NotFound();
    var taskCount = await db.TaskRows.CountAsync(t => t.ProjectId == id);
    return Results.Ok(new ProjectDto(p.Id, p.Name, p.Description, p.CreatedAt, p.OwnerId,
        p.Owner?.FullName, p.Owner?.Email, taskCount));
});

projects.MapPost("/", async ([FromBody] ProjectCreateDto dto, ProjectsDbContext db) =>
{
    if (!MiniValidator.TryValidate(dto, out var errors)) return Results.ValidationProblem(errors);
    var ownerExists = await db.Owners.AnyAsync(u => u.Id == dto.OwnerId);
    if (!ownerExists) return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["OwnerId"] = new[] { "Owner does not exist." }
    });
    var p = new Project
    {
        Name = dto.Name,
        Description = dto.Description,
        OwnerId = dto.OwnerId,
        CreatedAt = DateTime.UtcNow
    };
    db.Projects.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/api/projects/{p.Id}",
        new ProjectDto(p.Id, p.Name, p.Description, p.CreatedAt, p.OwnerId, null, null, 0));
});

projects.MapPut("/{id:int}", async (int id, [FromBody] ProjectUpdateDto dto, ProjectsDbContext db) =>
{
    if (!MiniValidator.TryValidate(dto, out var errors)) return Results.ValidationProblem(errors);
    var p = await db.Projects.FindAsync(id);
    if (p is null) return Results.NotFound();
    p.Name = dto.Name;
    p.Description = dto.Description;
    p.OwnerId = dto.OwnerId;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

projects.MapDelete("/{id:int}", async (int id, ProjectsDbContext db) =>
{
    var p = await db.Projects.FindAsync(id);
    if (p is null) return Results.NotFound();
    db.Projects.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Calendar feed: tasks in a given month, optionally scoped to one project.
app.MapGet("/api/calendar", async (int? year, int? month, int? projectId, ProjectsDbContext db) =>
{
    var today = DateTime.Today;
    var y = year ?? today.Year;
    var m = month ?? today.Month;
    var first = new DateTime(y, m, 1);
    var last = first.AddMonths(1).AddDays(-1);

    var query = db.TaskRows.AsNoTracking()
        .Where(t => t.DueDate >= first && t.DueDate <= last);
    if (projectId is int pid) query = query.Where(t => t.ProjectId == pid);

    // Materialise the join first, project to DTO in memory (a conditional
    // string inside the join doesn't translate to SQLite SQL).
    var raw = await query
        .Join(db.Projects, t => t.ProjectId, p => p.Id,
              (t, p) => new { t.Id, t.Title, t.DueDate, t.Status, t.ProjectId, ProjectName = p.Name })
        .OrderBy(x => x.DueDate)
        .ToListAsync();
    var tasks = raw.Select(x => new CalendarTaskDto(
        x.Id, x.Title, x.DueDate,
        x.Status switch { 1 => "InProgress", 2 => "Done", 3 => "Blocked", _ => "Todo" },
        x.ProjectId, x.ProjectName)).ToList();
    return Results.Ok(new { year = y, month = m, tasks });
}).RequireAuthorization();

// Owner lookup for the SPA's "owner" dropdown
app.MapGet("/api/owners", async (ProjectsDbContext db) =>
{
    var owners = await db.Owners.AsNoTracking()
        .OrderBy(u => u.FullName)
        .Select(u => new OwnerDto(u.Id, u.FullName, u.Email))
        .ToListAsync();
    return Results.Ok(owners);
}).RequireAuthorization();

app.Run();

internal static class MiniValidator
{
    public static bool TryValidate<T>(T instance, out IDictionary<string, string[]> errors) where T : class
    {
        var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(instance);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var ok = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            instance, ctx, results, validateAllProperties: true);
        errors = results
            .SelectMany(r => r.MemberNames.Select(m => (Member: m, r.ErrorMessage)))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid").ToArray());
        return ok;
    }
}
