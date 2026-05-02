using Clients.Domain.Entities;
using Deals.Application.Abstractions.Lookups;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deals.Infrastructure.Lookups;

public sealed class ClientLookupService : IClientLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public ClientLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsActiveAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<Client>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.Id == clientId &&
                     x.IsActive,
                cancellationToken);
    }
}
