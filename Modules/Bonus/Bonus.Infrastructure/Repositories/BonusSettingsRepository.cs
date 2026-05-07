using Bonus.Application.Abstractions.Repositories;
using Bonus.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bonus.Infrastructure.Repositories;

public sealed class BonusSettingsRepository : IBonusSettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BonusSettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BonusSettings settings, CancellationToken cancellationToken)
    {
        await _dbContext.Set<BonusSettings>().AddAsync(settings, cancellationToken);
    }

    public Task<BonusSettings?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<BonusSettings>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<BonusSettings>()
            .AnyAsync(x => x.OrganizationId == organizationId, cancellationToken);
    }
}
