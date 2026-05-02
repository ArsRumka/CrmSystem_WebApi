using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Common;
using Identity.Application.Contracts;
using MediatR;

namespace Identity.Application.Users;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserResponse>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRoleRepository _moduleRoleRepository;
    private readonly IModuleRepository _moduleRepository;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IModuleRoleRepository moduleRoleRepository,
        IModuleRepository moduleRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _moduleRoleRepository = moduleRoleRepository;
        _moduleRepository = moduleRepository;
    }

    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = HandlerGuards.RequireUserId(_currentUserService);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found");

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role was not found");

        var permissions = await GetRolePermissionsAsync(role.Id, cancellationToken);
        var roleResponse = new RoleResponse(role.Id, role.OrganizationId, role.Name, permissions);

        return new CurrentUserResponse(user.Id, user.OrganizationId, user.Name, user.Email, roleResponse, permissions);
    }

    private async Task<IReadOnlyList<RolePermissionResponse>> GetRolePermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var moduleRoles = await _moduleRoleRepository.GetByRoleIdAsync(roleId, cancellationToken);
        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        var modulesById = modules.ToDictionary(module => module.Id);

        return moduleRoles
            .Where(moduleRole => modulesById.ContainsKey(moduleRole.ModuleId))
            .Select(moduleRole =>
            {
                var module = modulesById[moduleRole.ModuleId];
                return new RolePermissionResponse(
                    module.Id,
                    module.Code,
                    module.Name,
                    moduleRole.CanRead,
                    moduleRole.CanCreate,
                    moduleRole.CanUpdate,
                    moduleRole.CanDelete);
            })
            .ToList();
    }
}
