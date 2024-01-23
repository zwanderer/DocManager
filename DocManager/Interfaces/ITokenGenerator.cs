using DocManager.DTOs;
using DocManager.Models;

namespace DocManager.Interfaces;

/// <summary>
/// Abstracts the Jwt Token generation service.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates a JWT token for an user.
    /// </summary>
    /// <param name="user">User instance to generate the token.</param>
    /// <returns>A signed JWT Token</returns>
    public AuthOutputDTO GenerateToken(UserModel user);
}
