using Bonus.Domain.Entities;

namespace Bonus.Application.Abstractions.Repositories;

public interface IBonusAccountRepository
{
    Task AddAsync(BonusAccount account, CancellationToken cancellationToken);

    Task<BonusAccount?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<BonusAccount?> GetByClientIdAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken);

    Task<List<BonusAccount>> SearchAsync(
        Guid organizationId,
        Guid? clientId,
        bool? isActive,
        CancellationToken cancellationToken);
}
