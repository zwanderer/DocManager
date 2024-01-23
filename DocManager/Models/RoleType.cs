namespace DocManager.Models;

/// <summary>
/// Enumerates the possible role types.
/// User = Can upload/download documents.
/// Admin = Can upload/download documents and manage users.
/// </summary>
public enum RoleType
{
    /// <summary>
    /// Can only upload/download documents.
    /// </summary>
    User = 1,

    /// <summary>
    /// Can upload/download documents and manage users.
    /// </summary>
    Admin = 2
}