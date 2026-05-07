using Bonus.Domain.Entities;
using Bonus.Domain.Enums;

namespace Bonus.Application.Abstractions.Repositories;

public interface IBonusTransactionRepository
{
    Task AddAsync(BonusTransaction transaction, CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<BonusTransaction> transactions, CancellationToken cancellationToken);

    Task<BonusTransaction?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAutomatedForDealAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken);

    Task<List<BonusTransaction>> SearchAsync(
        Guid organizationId,
        Guid? bonusAccountId,
        Guid? clientId,
        Guid? dealId,
        BonusTransactionType? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken);
}
