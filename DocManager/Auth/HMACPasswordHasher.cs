using System.Security.Cryptography;
using System.Text;

using DocManager.Interfaces;

namespace DocManager.Auth;

/// <summary>
/// Implementation of <see cref="IPasswordHasher"/> that uses HMAC-SHA-512
/// </summary>
public class HMACPasswordHasher(IConfiguration config) : IPasswordHasher
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(config["PasswordSecret"]!);

    /// <inheritdoc/>
    public string HashPassword(string email, string password)
    {
        byte[] input = Encoding.UTF8.GetBytes($"{email.ToLower()}//{password}");
        using var hasher = new HMACSHA512(_key);
        byte[] output = hasher.ComputeHash(input);
        return Convert.ToBase64String(output);
    }
}
