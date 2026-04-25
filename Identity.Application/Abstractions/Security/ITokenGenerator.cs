namespace Identity.Application.Abstractions.Security;

public interface ITokenGenerator
{
    string GenerateSecureToken();
}
