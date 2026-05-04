using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public record AuthUserDto(string Id, string? Email, string? FullName);
public record TokenResponse(string AccessToken, DateTime ExpiresAt, AuthUserDto User);
