// Ignore Spelling: Auth

using System.ComponentModel.DataAnnotations;

using DocManager.Models;

namespace DocManager.DTOs;

/// <summary>
/// A class that describes the input for authenticating an user.
/// </summary>
public class AuthInputDTO
{
    /// <summary>
    /// User's email.
    /// </summary>
    /// <example>admin@email.com</example>
    [Required]
    [MaxLength(255)]
    [MinLength(4)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// User's password.
    /// </summary>
    /// <example>password</example>
    [Required]
    [MaxLength(255)]
    [MinLength(8)]
    public string? Password { get; set; }
}

/// <summary>
/// A class that describes the token data that will be returned by a successful authentication.
/// </summary>
public class AuthOutputDTO
{
    /// <summary>
    /// Unique Id of the authenticated user.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    public Guid UserId { get; init; }

    /// <summary>
    /// Jwt Token
    /// </summary>
    /// <example>eyJhbGciOiJIUzUxMiIsInR5cC...</example>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Token Expiration
    /// </summary>
    /// <example>2023-01-01T23:23:23.123Z</example>
    public DateTime Expires { get; init; }

    /// <summary>
    /// Roles associated with the authenticated user.
    /// </summary>
    /// <example>["User","Admin"]</example>
    public ISet<RoleType> Roles { get; init; } = default!;
}
