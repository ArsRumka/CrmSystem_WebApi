using Chat.Application.Abstractions.Lookups;
using Identity.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Lookups;

public sealed class ChatOrganizationLookupService : IChatOrganizationLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public ChatOrganizationLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Guid?> GetOrganizationIdByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();

        return _dbContext.Set<Organization>()
            .AsNoTracking()
            .Where(x => x.Email == normalizedEmail && x.IsActive)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsActiveAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Organization>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == organizationId && x.IsActive, cancellationToken);
    }

    public Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Organization>()
            .AsNoTracking()
            .Where(x => x.Id == organizationId)
            .Select(x => (string?)x.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<string?> GetOrganizationEmailAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Organization>()
            .AsNoTracking()
            .Where(x => x.Id == organizationId)
            .Select(x => (string?)x.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
