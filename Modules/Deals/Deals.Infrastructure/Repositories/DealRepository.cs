using Deals.Application.Abstractions.Repositories;
using Deals.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deals.Infrastructure.Repositories;

public sealed class DealRepository : IDealRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DealRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Deal deal, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Deal>().AddAsync(deal, cancellationToken);
    }

    public Task<Deal?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Deal>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public Task<Deal?> GetByIdWithItemsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Deal>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public Task<Deal?> GetByIdWithItemsAndHistoryAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<Deal>()
            .Include(x => x.Items)
            .Include(x => x.StageHistory)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<Deal>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? clientId,
        Guid? responsibleUserId,
        Guid? stageId,
        DateTime? dateFrom,
        DateTime? dateTo,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Deal>()
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => x.Notes != null && EF.Functions.ILike(x.Notes, pattern));
        }

        if (clientId.HasValue)
        {
            query = query.Where(x => x.ClientId == clientId.Value);
        }

        if (responsibleUserId.HasValue)
        {
            query = query.Where(x => x.ResponsibleUserId == responsibleUserId.Value);
        }

        if (stageId.HasValue)
        {
            query = query.Where(x => x.StageId == stageId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dateTo.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsActiveByStageIdAsync(
        Guid organizationId,
        Guid stageId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<Deal>()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.StageId == stageId &&
                     x.IsActive,
                cancellationToken);
    }
}
