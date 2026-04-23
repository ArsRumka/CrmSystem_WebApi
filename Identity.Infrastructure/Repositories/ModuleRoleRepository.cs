using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class ModuleRoleRepository : IModuleRoleRepository
{
    private readonly AppDbContext _dbContext;

    public ModuleRoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<ModuleRole>()
            .AnyAsync(x => x.RoleId == roleId && x.ModuleId == moduleId, cancellationToken);
    }

    public async Task AddAsync(ModuleRole moduleRole, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ModuleRole>().AddAsync(moduleRole, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ModuleRole> moduleRoles, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ModuleRole>().AddRangeAsync(moduleRoles, cancellationToken);
    }
}
