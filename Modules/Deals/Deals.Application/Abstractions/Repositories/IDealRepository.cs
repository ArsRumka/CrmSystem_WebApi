using Deals.Domain.Entities;

namespace Deals.Application.Abstractions.Repositories;

public interface IDealRepository
{
    Task AddAsync(Deal deal, CancellationToken cancellationToken);

    Task<Deal?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<Deal?> GetByIdWithItemsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<Deal?> GetByIdWithItemsAndHistoryAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<Deal>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? clientId,
        Guid? responsibleUserId,
        Guid? stageId,
        DateTime? dateFrom,
        DateTime? dateTo,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<bool> ExistsActiveByStageIdAsync(Guid organizationId, Guid stageId, CancellationToken cancellationToken);
}
