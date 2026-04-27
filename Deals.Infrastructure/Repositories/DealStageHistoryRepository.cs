using Deals.Application.Abstractions.Repositories;
using Deals.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deals.Infrastructure.Repositories;

public sealed class DealStageHistoryRepository : IDealStageHistoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DealStageHistoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DealStageHistory history, CancellationToken cancellationToken)
    {
        await _dbContext.Set<DealStageHistory>().AddAsync(history, cancellationToken);
    }

    public async Task<List<DealStageHistory>> GetByDealIdAsync(
        Guid organizationId,
        Guid dealId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<DealStageHistory>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.DealId == dealId)
            .OrderBy(x => x.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
