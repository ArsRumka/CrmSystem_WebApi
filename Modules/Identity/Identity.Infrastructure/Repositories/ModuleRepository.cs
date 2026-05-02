using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class ModuleRepository : IModuleRepository
{
    private readonly AppDbContext _dbContext;

    public ModuleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Module>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Module?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Module>()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Module>()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Module>()
            .AnyAsync(x => x.Code == code, cancellationToken);
    }

    public async Task AddAsync(Module module, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Module>().AddAsync(module, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Module> modules, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Module>().AddRangeAsync(modules, cancellationToken);
    }
}
