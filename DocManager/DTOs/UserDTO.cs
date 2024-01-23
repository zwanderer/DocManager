using System.ComponentModel.DataAnnotations;

using DocManager.Models;

namespace DocManager.DTOs;

/// <summary>
/// A class that describes the user data that will be returned to consumers of User API.
/// </summary>
public class UserViewDTO
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Name of the user.
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Email of the user.
    /// </summary>
    /// <example>john.doe@email.com</example>
    public string Email { get; set; } = default!;

    /// <summary>
    /// Timestamp of user creation.
    /// </summary>
    /// <example>2024-01-01T01:01:01.111Z</example>
    public DateTimeOffset CreateTS { get; set; }

    /// <summary>
    /// Username of who created this user.
    /// </summary>
    /// <example>admin@email.com</example>
    public string CreatedBy { get; set; } = default!;

    /// <summary>
    /// Timestamp of last user update.
    /// </summary>
    /// <example>2024-01-01T01:01:01.111Z</example>
    public DateTimeOffset? UpdateTS { get; set; }

    /// <summary>
    /// Username of who last updated this user.
    /// </summary>
    /// <example>admin@email.com</example>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// A set of Role Types that are assigned to this User.
    /// </summary>
    /// <example>["Admin","User"]</example>
    public ISet<RoleType> RolesAssigned { get; set; } = default!;
}

/// <summary>
/// A class that describes the input for creating a new user.
/// </summary>
public class CreateUserDTO
{
    /// <summary>
    /// Name of the new user.
    /// </summary>
    /// <example>John Doe</example>
    [Required]
    [MaxLength(255)]
    [MinLength(1)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Email of the new user.
    /// </summary>
    /// <example>john.doe@email.com</example>
    [Required]
    [MaxLength(255)]
    [MinLength(4)]
    [EmailAddress]
    public string Email { get; set; } = default!;

    /// <summary>
    /// Password of the new user.
    /// </summary>
    /// <example>D0n7H4ckM3</example>
    [Required]
    [MaxLength(255)]
    [MinLength(8)]
    public string Password { get; set; } = default!;

    /// <summary>
    /// List of Role Types of the new user.
    /// </summary>
    /// <example>User</example>
    [Required]
    public ISet<RoleType> RolesAssigned { get; set; } = default!;
}

/// <summary>
/// A class that describes the input for updating an existing user.
/// </summary>
public class UpdateUserDTO
{
    /// <summary>
    /// New Name of the existing user.
    /// Won't be changed if omitted.
    /// </summary>
    /// <example>John Doe</example>
    [MaxLength(255)]
    [MinLength(1)]
    public string? Name { get; set; } = default!;

    /// <summary>
    /// New Password of the existing user.
    /// Won't be changed if omitted.
    /// </summary>
    /// <example>D0n7H4ckM3</example>
    [MaxLength(255)]
    [MinLength(8)]
    public string? Password { get; set; } = default!;

    /// <summary>
    /// New List of Role Types of the existing user.
    /// Won't be changed if omitted.
    /// </summary>
    /// <example>User</example>
    public ISet<RoleType>? RolesAssigned { get; set; } = default!;
}