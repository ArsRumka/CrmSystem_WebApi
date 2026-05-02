using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _dbContext;

    public OrganizationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Organization>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Organization?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Organization>()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Organization>()
            .AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Organization>().AddAsync(organization, cancellationToken);
    }
}
