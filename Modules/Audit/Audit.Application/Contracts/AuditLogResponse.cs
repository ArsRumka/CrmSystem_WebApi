using Audit.Domain.Entities;
using Audit.Domain.Enums;

namespace Audit.Application.Contracts;

public sealed record AuditLogResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? UserId,
    string ModuleCode,
    AuditAction Action,
    string EntityName,
    Guid? EntityId,
    string Description,
    string? OldValuesJson,
    string? NewValuesJson,
    DateTime CreatedAt,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId);

internal static class AuditLogResponseMapper
{
    public static AuditLogResponse ToResponse(this AuditLog log)
    {
        return new AuditLogResponse(
            log.Id,
            log.OrganizationId,
            log.UserId,
            log.ModuleCode,
            log.Action,
            log.EntityName,
            log.EntityId,
            log.Description,
            log.OldValuesJson,
            log.NewValuesJson,
            log.CreatedAt,
            log.IpAddress,
            log.UserAgent,
            log.CorrelationId);
    }
}

