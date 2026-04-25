using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Application.Abstractions.Time;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions, IDateTimeProvider dateTimeProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    public string GenerateAccessToken(User user, Role role, Organization organization)
    {
        var claims = new[]
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim("OrganizationId", organization.Id.ToString()),
            new Claim("RoleId", role.Id.ToString()),
            new Claim("Email", user.Email),
            new Claim("OrganizationEmail", organization.Email)
        };

        return GenerateToken(claims);
    }

    public string GenerateSystemAdminAccessToken(SystemAdmin systemAdmin)
    {
        var claims = new[]
        {
            new Claim("SystemAdminId", systemAdmin.Id.ToString()),
            new Claim("Email", systemAdmin.Email),
            new Claim("IsSystemAdmin", "true")
        };

        return GenerateToken(claims);
    }

    public DateTime GetAccessTokenExpiration()
    {
        return _dateTimeProvider.UtcNow.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
    }

    private string GenerateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = _dateTimeProvider.UtcNow;

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            now,
            GetAccessTokenExpiration(),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
