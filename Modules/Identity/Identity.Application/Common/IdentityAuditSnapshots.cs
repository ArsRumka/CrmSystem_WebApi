using Identity.Application.Contracts;
using Identity.Domain.Entities;

namespace Identity.Application.Common;

internal static class IdentityAuditSnapshots
{
    public static object User(User user)
    {
        return new
        {
            user.Name,
            user.Email,
            user.RoleId,
            user.IsActive,
            user.IsEmailConfirmed
        };
    }

    public static object Role(Role role, IEnumerable<RolePermissionResponse> permissions)
    {
        return new
        {
            role.Name,
            Permissions = permissions.Select(permission => new
            {
                permission.ModuleId,
                permission.ModuleCode,
                permission.CanRead,
                permission.CanCreate,
                permission.CanUpdate,
                permission.CanDelete
            }).ToList()
        };
    }

    public static object RolePermissions(IEnumerable<ModuleRole> permissions)
    {
        return permissions.Select(permission => new
        {
            permission.ModuleId,
            permission.CanRead,
            permission.CanCreate,
            permission.CanUpdate,
            permission.CanDelete
        }).ToList();
    }
}
