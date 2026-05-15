using Audit.Application.Abstractions.Repositories;
using Audit.Domain.Entities;
using Audit.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Audit.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken)
    {
        await _dbContext.Set<AuditLog>().AddAsync(log, cancellationToken);
    }

    public Task<AuditLog?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<AuditLog>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<AuditLog>> SearchAsync(
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
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(moduleCode))
        {
            query = query.Where(x => x.ModuleCode == moduleCode.Trim());
        }

        if (action.HasValue)
        {
            query = query.Where(x => x.Action == action.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName.Trim());
        }

        if (entityId.HasValue)
        {
            query = query.Where(x => x.EntityId == entityId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dateTo.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

