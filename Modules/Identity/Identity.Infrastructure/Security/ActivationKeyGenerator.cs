using System.Security.Cryptography;
using Identity.Application.Abstractions.Security;

namespace Identity.Infrastructure.Security;

public sealed class ActivationKeyGenerator : IActivationKeyGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Generate()
    {
        var parts = Enumerable.Range(0, 4)
            .Select(_ => GeneratePart())
            .ToArray();

        return $"CRM-{string.Join("-", parts)}";
    }

    private static string GeneratePart()
    {
        Span<char> chars = stackalloc char[4];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}
