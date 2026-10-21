using System.Security.Cryptography;
using System.Text;

namespace LearningPortal.Services;

public static class PasswordHelper
{
    // Same hashing as the SQL script: SHA-256, lowercase hex.
    public static string Hash(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();
}