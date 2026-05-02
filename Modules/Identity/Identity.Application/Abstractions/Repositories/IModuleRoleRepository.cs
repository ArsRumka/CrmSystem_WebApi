using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IModuleRoleRepository
{
    Task<ModuleRole?> GetByRoleAndModuleAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModuleRole>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken = default);

    Task AddAsync(ModuleRole moduleRole, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<ModuleRole> moduleRoles, CancellationToken cancellationToken = default);

    void DeleteRange(IEnumerable<ModuleRole> moduleRoles);
}
