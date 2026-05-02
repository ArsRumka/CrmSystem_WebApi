using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class SystemAdminRepository : ISystemAdminRepository
{
    private readonly AppDbContext _dbContext;

    public SystemAdminRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SystemAdmin systemAdmin, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<SystemAdmin>().AddAsync(systemAdmin, cancellationToken);
    }

    public Task<SystemAdmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<SystemAdmin>()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<SystemAdmin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<SystemAdmin>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<SystemAdmin>()
            .AnyAsync(x => x.Email == email, cancellationToken);
    }
}
