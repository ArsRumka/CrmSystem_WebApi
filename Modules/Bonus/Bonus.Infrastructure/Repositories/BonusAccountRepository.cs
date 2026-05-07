using Bonus.Application.Abstractions.Repositories;
using Bonus.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bonus.Infrastructure.Repositories;

public sealed class BonusAccountRepository : IBonusAccountRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BonusAccountRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BonusAccount account, CancellationToken cancellationToken)
    {
        await _dbContext.Set<BonusAccount>().AddAsync(account, cancellationToken);
    }

    public Task<BonusAccount?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<BonusAccount>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == id,
                cancellationToken);
    }

    public Task<BonusAccount?> GetByClientIdAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<BonusAccount>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.ClientId == clientId,
                cancellationToken);
    }

    public async Task<List<BonusAccount>> SearchAsync(
        Guid organizationId,
        Guid? clientId,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<BonusAccount>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (clientId.HasValue)
        {
            query = query.Where(x => x.ClientId == clientId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(x => x.ClientId)
            .ToListAsync(cancellationToken);
    }
}
