using System.ComponentModel.DataAnnotations;

namespace DocManager.Models;

/// <summary>
/// An entity that represents the person who interacts with the system.
/// </summary>
public class UserModel
{
    /// <summary>
    /// Unique identifier of the User.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    [Key]
    public Guid UserId { get; set; }

    /// <summary>
    /// Name of the User.
    /// </summary>
    /// <example>John Doe</example>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email of the User.
    /// </summary>
    /// <example>john.doe@email.com</example>
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Base64 encoded password hash. Uses HMAC SHA-512.
    /// </summary>
    /// <example>4Nvxz4qNuyUAKaOWbfggo/zVejlOAh9RONyp/CzeeCU=</example>
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of when the User was created.
    /// </summary>
    /// <example>2023-01-02T22:12:00.000Z</example>
    [Required]
    public DateTimeOffset CreateTS { get; set; }

    /// <summary>
    /// User Id of the person who created this User.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    [Required]
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Timestamp of when the User was last modified.
    /// </summary>
    /// <example>2023-01-02T22:12:00.000Z</example>
    public DateTimeOffset? UpdateTS { get; set; }

    /// <summary>
    /// User Id of the person who last modified this User.
    /// </summary>
    /// <example>51a1d376-f02c-4b22-84f9-26e8ce250e43</example>
    public Guid? UpdatedByUserID { get; set; }

    /// <summary>
    /// A set of Role Types that are assigned to this User.
    /// </summary>
    /// <example>Admin;User</example>
    public ISet<RoleType> RolesAssigned { get; set; } = default!;
}
