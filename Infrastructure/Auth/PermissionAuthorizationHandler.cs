using System.Security.Claims;
using Identity.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;

    public PermissionAuthorizationHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue("UserId");
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        if (await _permissionService.HasPermissionAsync(userId, requirement.ModuleCode, requirement.Action))
        {
            context.Succeed(requirement);
        }
    }
}
