using Identity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string moduleCode, PermissionAction action)
    {
        ModuleCode = moduleCode;
        Action = action;
    }

    public string ModuleCode { get; }
    public PermissionAction Action { get; }
}
