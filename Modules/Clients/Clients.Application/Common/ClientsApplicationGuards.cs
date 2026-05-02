using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;

namespace Clients.Application.Common;

internal static class ClientsApplicationGuards
{
    public static Guid RequireOrganizationUser(ICurrentUserService currentUserService)
    {
        if (!currentUserService.IsAuthenticated ||
            currentUserService.UserId is null ||
            currentUserService.OrganizationId is null)
        {
            throw new UnauthorizedException("Organization user is not authenticated");
        }

        return currentUserService.OrganizationId.Value;
    }
}
