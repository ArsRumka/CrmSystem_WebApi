using System.Security.Cryptography;
using System.Text;
using Identity.Application.Abstractions.Security;

namespace Identity.Infrastructure.Security;

public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool Verify(string token, string tokenHash)
    {
        var computedHash = Hash(token);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(tokenHash));
    }
}
