namespace Auth.Api.Models;

/// <summary>
/// Read-only mapping over the monolith's AspNetUsers table — only the columns
/// we need to authenticate. Auth.Api never writes to this table; password
/// changes and registrations still flow through the MVC monolith.
///
/// PasswordHash uses ASP.NET Identity v3 format (PBKDF2 + HMAC-SHA256, 100k
/// iterations, 128-bit salt). We verify it with the same PasswordHasher class
/// the monolith uses, so hashes are bit-for-bit compatible without us
/// re-implementing crypto.
/// </summary>
public class User
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? PasswordHash { get; set; }
    public string? FullName { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
}
