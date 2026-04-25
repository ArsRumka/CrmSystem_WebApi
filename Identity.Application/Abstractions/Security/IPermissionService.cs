using Identity.Domain.Enums;

namespace Identity.Application.Abstractions.Security;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        string moduleCode,
        PermissionAction action,
        CancellationToken cancellationToken = default);
}
