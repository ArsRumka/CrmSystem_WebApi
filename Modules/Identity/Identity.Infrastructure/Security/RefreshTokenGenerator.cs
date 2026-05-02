using BuildingBlocks.Application.Abstractions.Time;
using Identity.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _jwtOptions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenGenerator(IOptions<JwtOptions> jwtOptions, IDateTimeProvider dateTimeProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    public string Generate()
    {
        return SecureTokenGenerator.GenerateToken(64);
    }

    public DateTime GetExpiration()
    {
        return _dateTimeProvider.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays);
    }
}
