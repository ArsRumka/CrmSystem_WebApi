using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Application.Abstractions.Repositories;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<Product>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? categoryId,
        bool? isActive,
        BonusType? bonusType,
        DiscountType? discountType,
        CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
}
