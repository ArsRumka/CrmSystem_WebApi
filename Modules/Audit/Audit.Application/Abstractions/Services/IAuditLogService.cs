using Audit.Domain.Enums;

namespace Audit.Application.Abstractions.Services;

public interface IAuditLogService
{
    Task LogAsync(
        Guid organizationId,
        Guid? userId,
        string moduleCode,
        AuditAction action,
        string entityName,
        Guid? entityId,
        string description,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken);

    Task LogAsync(AuditLogCreateRequest request, CancellationToken cancellationToken);
}

public sealed record AuditLogCreateRequest(
    Guid OrganizationId,
    Guid? UserId,
    string ModuleCode,
    AuditAction Action,
    string EntityName,
    Guid? EntityId,
    string Description,
    object? OldValues,
    object? NewValues,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CorrelationId = null);

