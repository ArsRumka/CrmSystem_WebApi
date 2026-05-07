using Deals.Application.Abstractions.Repositories;
using Deals.Domain.Entities;
using Deals.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deals.Infrastructure.Repositories;

public sealed class DealReturnRepository : IDealReturnRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DealReturnRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DealReturn dealReturn, CancellationToken cancellationToken)
    {
        await _dbContext.Set<DealReturn>().AddAsync(dealReturn, cancellationToken);
    }

    public Task<DealReturn?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<DealReturn>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == id,
                cancellationToken);
    }

    public Task<DealReturn?> GetByIdWithItemsAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<DealReturn>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == id,
                cancellationToken);
    }

    public async Task<List<DealReturn>> GetByDealIdAsync(
        Guid organizationId,
        Guid dealId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<DealReturn>()
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.OrganizationId == organizationId && x.DealId == dealId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DealReturn>> GetCompletedByDealIdAsync(
        Guid organizationId,
        Guid dealId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<DealReturn>()
            .AsNoTracking()
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.DealId == dealId &&
                x.Status == DealReturnStatus.Completed)
            .OrderBy(x => x.CompletedAt)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DealReturnItem>> GetCompletedItemsByDealIdAsync(
        Guid organizationId,
        Guid dealId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<DealReturnItem>()
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId &&
                item.DealId == dealId &&
                _dbContext.Set<DealReturn>().Any(dealReturn =>
                    dealReturn.OrganizationId == organizationId &&
                    dealReturn.Id == item.DealReturnId &&
                    dealReturn.Status == DealReturnStatus.Completed))
            .ToListAsync(cancellationToken);
    }
}
