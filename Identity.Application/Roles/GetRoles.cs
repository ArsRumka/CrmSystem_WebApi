using BuildingBlocks.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Roles;

public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleResponse>>;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRoleRepository _moduleRoleRepository;
    private readonly IModuleRepository _moduleRepository;

    public GetRolesQueryHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IRoleRepository roleRepository,
        IModuleRoleRepository moduleRoleRepository,
        IModuleRepository moduleRepository)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _roleRepository = roleRepository;
        _moduleRoleRepository = moduleRoleRepository;
        _moduleRepository = moduleRepository;
    }

    public async Task<IReadOnlyList<RoleResponse>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.RolesModuleCode,
            PermissionAction.Read,
            cancellationToken);

        var roles = await _roleRepository.GetRolesByOrganizationIdAsync(organizationId, cancellationToken);
        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        var modulesById = modules.ToDictionary(module => module.Id);

        var result = new List<RoleResponse>();
        foreach (var role in roles)
        {
            var moduleRoles = await _moduleRoleRepository.GetByRoleIdAsync(role.Id, cancellationToken);
            result.Add(new RoleResponse(
                role.Id,
                role.OrganizationId,
                role.Name,
                MapPermissions(moduleRoles, modulesById)));
        }

        return result;
    }

    private static IReadOnlyList<RolePermissionResponse> MapPermissions(
        IEnumerable<Identity.Domain.Entities.ModuleRole> moduleRoles,
        IReadOnlyDictionary<Guid, Identity.Domain.Entities.Module> modulesById)
    {
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
