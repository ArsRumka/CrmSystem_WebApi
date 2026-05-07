using Bonus.Application.Abstractions.Repositories;
using Bonus.Domain.Entities;
using Bonus.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bonus.Infrastructure.Repositories;

public sealed class BonusTransactionRepository : IBonusTransactionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BonusTransactionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BonusTransaction transaction, CancellationToken cancellationToken)
    {
        await _dbContext.Set<BonusTransaction>().AddAsync(transaction, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<BonusTransaction> transactions, CancellationToken cancellationToken)
    {
        await _dbContext.Set<BonusTransaction>().AddRangeAsync(transactions, cancellationToken);
    }

    public Task<BonusTransaction?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<BonusTransaction>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == id,
                cancellationToken);
    }

    public Task<bool> ExistsAutomatedForDealAsync(
        Guid organizationId,
        Guid dealId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<BonusTransaction>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.DealId == dealId &&
                     (x.Type == BonusTransactionType.WriteOff ||
                      x.Type == BonusTransactionType.Accrual),
                cancellationToken);
    }

    public async Task<List<BonusTransaction>> SearchAsync(
        Guid organizationId,
        Guid? bonusAccountId,
        Guid? clientId,
        Guid? dealId,
        BonusTransactionType? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<BonusTransaction>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (bonusAccountId.HasValue)
        {
            query = query.Where(x => x.BonusAccountId == bonusAccountId.Value);
        }

        if (clientId.HasValue)
        {
            query = query.Where(x => x.ClientId == clientId.Value);
        }

        if (dealId.HasValue)
        {
            query = query.Where(x => x.DealId == dealId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
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
            .ToListAsync(cancellationToken);
    }
}
