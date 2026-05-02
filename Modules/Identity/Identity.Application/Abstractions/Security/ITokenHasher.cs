namespace Identity.Application.Abstractions.Security;

public interface ITokenHasher
{
    string Hash(string token);

    bool Verify(string token, string tokenHash);
}
