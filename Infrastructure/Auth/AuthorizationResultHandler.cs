using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public sealed class AuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Unauthorized",
                detail = "Authentication is required."
            });
            return;
        }

        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Forbidden",
                detail = GetForbiddenDetail(context, policy)
            });
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static string GetForbiddenDetail(HttpContext context, Microsoft.AspNetCore.Authorization.AuthorizationPolicy policy)
    {
        var requiresPermission = policy.Requirements.OfType<PermissionRequirement>().Any();
        var isSystemAdmin = string.Equals(
            context.User.FindFirst("IsSystemAdmin")?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (requiresPermission && isSystemAdmin)
        {
            return "System administrator tokens cannot access organization-user endpoints. Use an organization user token for this resource.";
        }

        if (requiresPermission)
        {
            return "The authenticated user does not have the required module permission.";
        }

        return "The authenticated user is not allowed to access this resource.";
    }
}
