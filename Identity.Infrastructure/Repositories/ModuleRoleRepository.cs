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

    public Task<ModuleRole?> GetByRoleAndModuleAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<ModuleRole>()
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.ModuleId == moduleId, cancellationToken);
    }

    public async Task<IReadOnlyList<ModuleRole>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ModuleRole>()
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);
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

    public void DeleteRange(IEnumerable<ModuleRole> moduleRoles)
    {
        _dbContext.Set<ModuleRole>().RemoveRange(moduleRoles);
    }
}
