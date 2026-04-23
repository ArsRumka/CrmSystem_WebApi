using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _dbContext;

    public RoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Role>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Role?> GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Role>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Name == name, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Role>()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Name == name, cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Role>().AddAsync(role, cancellationToken);
    }
}
