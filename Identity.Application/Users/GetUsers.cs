using BuildingBlocks.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Users;

public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserResponse>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.UsersModuleCode,
            PermissionAction.Read,
            cancellationToken);

        var users = await _userRepository.GetUsersByOrganizationIdAsync(organizationId, cancellationToken);

        return users
            .Select(user => new UserResponse(
                user.Id,
                user.OrganizationId,
                user.RoleId,
                user.Name,
                user.Email,
                user.IsActive,
                user.IsEmailConfirmed))
            .ToList();
    }
}
