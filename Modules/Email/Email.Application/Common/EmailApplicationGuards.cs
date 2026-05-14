using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;

namespace Email.Application.Common;

internal static class EmailApplicationGuards
{
    public static (Guid OrganizationId, Guid UserId) RequireOrganizationUser(ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated ||
            currentUserService.UserId is null ||
            currentUserService.OrganizationId is null)
        {
            throw new UnauthorizedException("Organization user is not authenticated");
        }

        return (currentUserService.OrganizationId.Value, currentUserService.UserId.Value);
    }
}
