// Ignore Spelling: Jwt

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using DocManager.DTOs;
using DocManager.Interfaces;
using DocManager.Models;

using Microsoft.IdentityModel.Tokens;

namespace DocManager.Auth;

/// <summary>
/// Implements <see cref="ITokenGenerator"/> using JwtSecurityTokenHandler.
/// </summary>
public class JwtTokenGenerator(IConfiguration config) : ITokenGenerator
{
    private readonly IConfiguration _config = config;

    /// <inheritdoc/>
    public AuthOutputDTO GenerateToken(UserModel user)
    {
        string issuer = _config["Jwt:Issuer"]!;
        string audience = _config["Jwt:Audience"]!;
        byte[] secret = Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!);

        var subject = new List<Claim>()
        {
            new("id", user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Email),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var roleType in user.RolesAssigned)
            subject.Add(new Claim("roles", roleType.ToString()));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(subject),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha512Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        string jwtToken = tokenHandler.WriteToken(token);

        return new AuthOutputDTO
        {
            UserId = user.UserId,
            Token = jwtToken,
            Expires = token.ValidTo,
            Roles = user.RolesAssigned
        };
    }
}
