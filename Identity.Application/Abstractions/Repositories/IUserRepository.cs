using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(Guid organizationId, string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(Guid organizationId, string email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
