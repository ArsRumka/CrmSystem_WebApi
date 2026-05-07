using Bonus.Application.Abstractions.Lookups;
using Clients.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bonus.Infrastructure.Services;

public sealed class BonusClientLookupService : IBonusClientLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public BonusClientLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Client>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId && x.Id == clientId,
                cancellationToken);
    }
}
