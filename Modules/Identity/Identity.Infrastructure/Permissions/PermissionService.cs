using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;

namespace Identity.Infrastructure.Permissions;

public sealed class PermissionService : IPermissionService
{
    private readonly IUserRepository _userRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IModuleRoleRepository _moduleRoleRepository;

    public PermissionService(
        IUserRepository userRepository,
        IModuleRepository moduleRepository,
        IModuleRoleRepository moduleRoleRepository)
    {
        _userRepository = userRepository;
        _moduleRepository = moduleRepository;
        _moduleRoleRepository = moduleRoleRepository;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string moduleCode,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var module = await _moduleRepository.GetByCodeAsync(moduleCode, cancellationToken);
        if (module is null)
        {
            return false;
        }

        var moduleRole = await _moduleRoleRepository.GetByRoleAndModuleAsync(user.RoleId, module.Id, cancellationToken);
        if (moduleRole is null)
        {
            return false;
        }

        return action switch
        {
            PermissionAction.Read => moduleRole.CanRead,
            PermissionAction.Create => moduleRole.CanCreate,
            PermissionAction.Update => moduleRole.CanUpdate,
            PermissionAction.Delete => moduleRole.CanDelete,
            _ => false
        };
    }
}
