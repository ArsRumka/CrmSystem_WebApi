using System.Security.Cryptography;
using Identity.Application.Abstractions.Security;

namespace Identity.Infrastructure.Security;

public sealed class SecureTokenGenerator : ITokenGenerator
{
    public string GenerateSecureToken()
    {
        return GenerateToken(32);
    }

    internal static string GenerateToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
