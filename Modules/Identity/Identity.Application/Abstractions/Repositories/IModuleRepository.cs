using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IModuleRepository
{
    Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Module?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(Module module, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Module> modules, CancellationToken cancellationToken = default);
}
