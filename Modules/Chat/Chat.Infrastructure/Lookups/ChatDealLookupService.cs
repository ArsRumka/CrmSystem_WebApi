using Chat.Application.Abstractions.Lookups;
using Deals.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Lookups;

public sealed class ChatDealLookupService : IChatDealLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public ChatDealLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Deal>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.Id == dealId &&
                     x.IsActive,
                cancellationToken);
    }
}
