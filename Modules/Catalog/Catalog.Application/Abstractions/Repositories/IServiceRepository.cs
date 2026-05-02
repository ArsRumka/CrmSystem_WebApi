using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Application.Abstractions.Repositories;

public interface IServiceRepository
{
    Task AddAsync(Service service, CancellationToken cancellationToken);

    Task<Service?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<Service>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? categoryId,
        bool? isActive,
        BonusType? bonusType,
        DiscountType? discountType,
        CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
}
