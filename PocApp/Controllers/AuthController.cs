using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PocApp.Models;

namespace PocApp.Controllers;

/// <summary>
/// Bridges Identity-cookie auth (used by the MVC monolith) with JWT bearer auth
/// (used by the Projects.Api microservice + the React SPA). The user signs in once
/// via the MVC Identity flow; the SPA then calls /api/auth/token (carrying the
/// Identity cookie) to obtain a short-lived JWT for API calls.
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string? returnUrl = "/")
    {
        await _signInManager.SignOutAsync();
        return Redirect(returnUrl ?? "/");
    }

    [HttpGet("token")]
    [Authorize] // requires valid Identity cookie
    public async Task<IActionResult> Token()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresMinutes = int.Parse(jwt["ExpiresMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = token.ValidTo,
            user = new { id = user.Id, email = user.Email, fullName = user.FullName }
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Ok(new { authenticated = false });
        var user = await _userManager.GetUserAsync(User);
        return Ok(new
        {
            authenticated = true,
            id = user?.Id,
            email = user?.Email,
            fullName = user?.FullName
        });
    }
}
