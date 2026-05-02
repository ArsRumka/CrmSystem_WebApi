using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface ISystemAdminRepository
{
    Task AddAsync(SystemAdmin systemAdmin, CancellationToken cancellationToken = default);

    Task<SystemAdmin?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<SystemAdmin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}
