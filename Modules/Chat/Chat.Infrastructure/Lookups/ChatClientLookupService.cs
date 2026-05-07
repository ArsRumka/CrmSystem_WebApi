using Chat.Application.Abstractions.Lookups;
using Clients.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Lookups;

public sealed class ChatClientLookupService : IChatClientLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public ChatClientLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken)
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
