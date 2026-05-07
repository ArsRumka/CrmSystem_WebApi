using Chat.Application.Abstractions.Lookups;
using Identity.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Lookups;

public sealed class ChatUserLookupService : IChatUserLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public ChatUserLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsActiveInOrganizationAsync(
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

    public Task<string?> GetUserDisplayNameAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<User>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Id == userId)
            .Select(x => (string?)x.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
