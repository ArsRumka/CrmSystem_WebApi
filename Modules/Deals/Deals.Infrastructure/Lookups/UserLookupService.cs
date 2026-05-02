using Deals.Application.Abstractions.Lookups;
using Identity.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deals.Infrastructure.Lookups;

public sealed class UserLookupService : IUserLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public UserLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsActiveAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<User>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.Id == userId &&
                     x.IsActive,
                cancellationToken);
    }
}
