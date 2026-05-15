using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;

namespace Audit.Application.Common;

internal static class AuditApplicationGuards
{
    public static Guid RequireOrganizationUser(ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.OrganizationId is null)
        {
            throw new UnauthorizedException("Organization user is not authenticated");
        }

        return currentUserService.OrganizationId.Value;
    }
}

