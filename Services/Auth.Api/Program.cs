using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Auth.Api.Contracts;
using Auth.Api.Data;
using Auth.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ----- DB: shares the monolith's SQLite file (read-only access to AspNetUsers) -----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AuthDbContext>(o => o.UseSqlite(connectionString));

// IPasswordHasher<User> uses ASP.NET Identity v3 format by default — bit-for-bit
// compatible with the monolith's PasswordHasher<ApplicationUser>. We never call
// HashPassword (we don't write); we only call VerifyHashedPassword.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// ----- HTTP client for the cookie-validation trampoline -----
// Auth.Api doesn't share Data Protection keys with MVC (the monolith uses
// ephemeral keys in dev). Instead, when /api/auth/exchange is called we
// forward the incoming cookie to MVC's existing /api/auth/me endpoint and
// trust the answer. This works because MVC validates its own cookie inside
// the process that owns the DP keys. ZERO MVC CHANGE.
builder.Services.AddHttpClient("mvc", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Mvc:BaseUrl"] ?? "http://localhost:5205/");
    c.Timeout = TimeSpan.FromSeconds(5);
});

// ----- JWT bearer (key/issuer/audience must match Projects.Api) -----
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
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

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "Auth.Api" }));

app.MapPost("/api/auth/login",
    async ([FromBody] LoginRequest req,
           AuthDbContext db,
           IPasswordHasher<User> hasher,
           IConfiguration cfg,
           ILogger<Program> log) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["_"] = new[] { "Email and password are required." }
        });

    var normalizedEmail = req.Email.Trim().ToUpperInvariant();
    var user = await db.Users.AsNoTracking()
        .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

    if (user is null || string.IsNullOrEmpty(user.PasswordHash))
    {
        log.LogInformation("Login failed: no user for {Email}", req.Email);
        return Results.Unauthorized();
    }

    // Lockout check (the monolith maintains these counters; we read them).
    if (user.LockoutEnabled && user.LockoutEnd is { } until && until > DateTimeOffset.UtcNow)
    {
        log.LogInformation("Login refused: {Email} locked out until {Until}", req.Email, until);
        return Results.Unauthorized();
    }

    var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
    if (verify == PasswordVerificationResult.Failed)
    {
        log.LogInformation("Login failed: bad password for {Email}", req.Email);
        return Results.Unauthorized();
    }
    // SuccessRehashNeeded = legacy format; accept it. The monolith rehashes
    // on its own login flow; we don't write to the user table.

    var token = MintJwt(user, cfg, signingKey);
    log.LogInformation("Issued JWT for {Email}", user.Email);
    return Results.Ok(token);
});

app.MapPost("/api/auth/refresh",
    async (HttpContext ctx,
           AuthDbContext db,
           IConfiguration cfg) =>
{
    // [Authorize] requires a still-valid JWT. We re-read the user (in case the
    // monolith disabled the account in the interim) and mint a fresh token.
    var sub = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (string.IsNullOrEmpty(sub)) return Results.Unauthorized();
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == sub);
    if (user is null) return Results.Unauthorized();
    return Results.Ok(MintJwt(user, cfg, signingKey));
}).RequireAuthorization();

// SSO bridge — "MVC first → React" path.
// If the browser already has a valid MVC Identity cookie (user signed in at
// /Identity/Account/Login), Auth.Api forwards the cookie to MVC's existing
// /api/auth/me endpoint. MVC, which owns the DP keys, validates the cookie
// in-process and answers with the user identity. We then mint a JWT.
app.MapPost("/api/auth/exchange",
    async (HttpContext ctx,
           AuthDbContext db,
           IHttpClientFactory httpFactory,
           IConfiguration cfg,
           ILogger<Program> log) =>
{
    // Pull the Identity cookie from the incoming request and forward it to MVC.
    if (!ctx.Request.Cookies.TryGetValue(".AspNetCore.Identity.Application", out var cookieValue)
        || string.IsNullOrEmpty(cookieValue))
    {
        return Results.Unauthorized();
    }

    var http = httpFactory.CreateClient("mvc");
    using var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
    req.Headers.Add("Cookie", $".AspNetCore.Identity.Application={cookieValue}");

    HttpResponseMessage mvcRes;
    try { mvcRes = await http.SendAsync(req); }
    catch (Exception ex)
    {
        log.LogWarning(ex, "MVC trampoline call failed");
        return Results.Unauthorized();
    }
    if (!mvcRes.IsSuccessStatusCode) return Results.Unauthorized();

    var json = await mvcRes.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    if (!root.TryGetProperty("authenticated", out var authProp) || !authProp.GetBoolean())
        return Results.Unauthorized();

    var userId = root.GetProperty("id").GetString();
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
    if (user is null) return Results.Unauthorized();

    log.LogInformation("Exchanged MVC cookie → JWT for {Email}", user.Email);
    return Results.Ok(MintJwt(user, cfg, signingKey));
});

app.MapPost("/api/auth/logout", async (HttpContext ctx, IHttpClientFactory httpFactory) =>
{
    // 1. Stateless JWT — client discards its in-memory token (nothing for the
    //    server to do here).
    // 2. Tell MVC to sign out so the Identity cookie is invalidated. We also
    //    set Set-Cookie with an expired date locally as a belt-and-braces in
    //    case MVC's response doesn't propagate through the gateway perfectly.
    if (ctx.Request.Cookies.TryGetValue(".AspNetCore.Identity.Application", out var cookieValue)
        && !string.IsNullOrEmpty(cookieValue))
    {
        try
        {
            var http = httpFactory.CreateClient("mvc");
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/logout?returnUrl=/");
            req.Headers.Add("Cookie", $".AspNetCore.Identity.Application={cookieValue}");
            await http.SendAsync(req);
        }
        catch { /* best-effort */ }
    }
    ctx.Response.Cookies.Delete(".AspNetCore.Identity.Application", new CookieOptions
    {
        Path = "/",
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
    });
    return Results.Ok(new { ok = true });
});

app.MapGet("/api/auth/me", (HttpContext ctx) =>
{
    if (!(ctx.User.Identity?.IsAuthenticated ?? false))
        return Results.Ok(new { authenticated = false });
    return Results.Ok(new
    {
        authenticated = true,
        id = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub),
        email = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Email),
        fullName = ctx.User.FindFirstValue("fullName")
    });
});

app.Run();

static TokenResponse MintJwt(User user, IConfiguration cfg, SymmetricSecurityKey key)
{
    var jwtSection = cfg.GetSection("Jwt");
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expiresMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "60");

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new("fullName", user.FullName ?? string.Empty)
    };

    var token = new JwtSecurityToken(
        issuer: jwtSection["Issuer"],
        audience: jwtSection["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
        signingCredentials: creds);

    return new TokenResponse(
        new JwtSecurityTokenHandler().WriteToken(token),
        token.ValidTo,
        new AuthUserDto(user.Id, user.Email, user.FullName));
}
