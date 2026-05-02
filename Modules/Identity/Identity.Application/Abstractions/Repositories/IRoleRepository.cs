using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetRolesByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    void Delete(Role role);
}
