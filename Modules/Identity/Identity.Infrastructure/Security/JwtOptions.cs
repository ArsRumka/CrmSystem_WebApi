namespace Identity.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "CrmSystem";
    public string Audience { get; init; } = "CrmSystem";
    public string Secret { get; init; } = "CHANGE_ME_TO_LONG_SECRET_KEY_FOR_DEVELOPMENT_ONLY";
    public int AccessTokenLifetimeMinutes { get; init; } = 30;
    public int RefreshTokenLifetimeDays { get; init; } = 7;
}
