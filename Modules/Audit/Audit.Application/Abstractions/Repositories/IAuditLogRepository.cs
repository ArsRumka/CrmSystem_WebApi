using Audit.Domain.Entities;
using Audit.Domain.Enums;

namespace Audit.Application.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken);

    Task<AuditLog?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<AuditLog>> SearchAsync(
        Guid organizationId,
        string? moduleCode,
        AuditAction? action,
        string? entityName,
        Guid? entityId,
        Guid? userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int skip,
        int take,
        CancellationToken cancellationToken);
}

