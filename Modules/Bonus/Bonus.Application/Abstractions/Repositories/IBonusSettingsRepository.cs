using Bonus.Domain.Entities;

namespace Bonus.Application.Abstractions.Repositories;

public interface IBonusSettingsRepository
{
    Task AddAsync(BonusSettings settings, CancellationToken cancellationToken);

    Task<BonusSettings?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid organizationId, CancellationToken cancellationToken);
}
