using Identity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Presentation.Authorization;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string moduleCode, PermissionAction action)
    {
        Policy = $"Permission:{moduleCode}:{action}";
    }
}
