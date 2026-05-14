using Email.Application.Abstractions.Services;
using Identity.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Email.Infrastructure.Services;

public sealed class EmailOrganizationLookupService : IEmailOrganizationLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public EmailOrganizationLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Organization>()
            .AsNoTracking()
            .Where(x => x.Id == organizationId)
            .Select(x => (string?)x.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
