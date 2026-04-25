namespace Identity.Application.Abstractions.Security;

public interface IRefreshTokenGenerator
{
    string Generate();

    DateTime GetExpiration();
}
