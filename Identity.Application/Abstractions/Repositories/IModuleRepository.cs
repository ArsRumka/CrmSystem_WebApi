using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IModuleRepository
{
    Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Module?> GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken = default);

    Task AddAsync(Module module, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Module> modules, CancellationToken cancellationToken = default);
}
