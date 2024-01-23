namespace DocManager.Interfaces;

/// <summary>
/// Abstracts password hashing service.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Generates a Base64 encoded hash of a user's email and password.
    /// </summary>
    /// <param name="email">User's email</param>
    /// <param name="password">User's password</param>
    /// <returns>Base64 encoded hash of the password</returns>
    public string HashPassword(string email, string password);
}
