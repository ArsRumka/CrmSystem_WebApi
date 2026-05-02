using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<User>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(Guid organizationId, string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<User>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(Guid organizationId, string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<User>()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<User>()
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetUsersByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<User>()
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<User>().AddAsync(user, cancellationToken);
    }
}
