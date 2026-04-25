using System.Security.Claims;
using BuildingBlocks.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId => GetGuidClaim("UserId");

    public Guid? OrganizationId => GetGuidClaim("OrganizationId");

    public Guid? RoleId => GetGuidClaim("RoleId");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsSystemAdmin => string.Equals(GetStringClaim("IsSystemAdmin"), "true", StringComparison.OrdinalIgnoreCase);

    public Guid? SystemAdminId => GetGuidClaim("SystemAdminId");

    private Guid? GetGuidClaim(string claimType)
    {
        var value = GetStringClaim(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? GetStringClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
    }
}
