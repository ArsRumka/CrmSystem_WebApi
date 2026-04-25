using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;

namespace Identity.Application.Common;

internal static class HandlerGuards
{
    public static Guid RequireUserId(ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            throw new UnauthorizedException("User is not authenticated");
        }

        return currentUserService.UserId.Value;
    }

    public static Guid RequireOrganizationId(ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.OrganizationId is null)
        {
            throw new UnauthorizedException("Organization user is not authenticated");
        }

        return currentUserService.OrganizationId.Value;
    }

    public static Guid RequireSystemAdminId(ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.IsSystemAdmin || currentUserService.SystemAdminId is null)
        {
            throw new ForbiddenException("System administrator access is required");
        }

        return currentUserService.SystemAdminId.Value;
    }

    public static async Task EnsurePermissionAsync(
        IPermissionService permissionService,
        Guid userId,
        string moduleCode,
        PermissionAction action,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.HasPermissionAsync(userId, moduleCode, action, cancellationToken))
        {
            throw new ForbiddenException($"Permission '{moduleCode}/{action}' is required");
        }
    }
}
