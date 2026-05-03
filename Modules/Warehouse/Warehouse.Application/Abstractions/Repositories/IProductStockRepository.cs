using Warehouse.Domain.Entities;

namespace Warehouse.Application.Abstractions.Repositories;

public interface IProductStockRepository
{
    Task AddAsync(ProductStock stock, CancellationToken cancellationToken);

    Task<ProductStock?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<ProductStock?> GetByStorageAndProductAsync(
        Guid organizationId,
        Guid storageId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<List<ProductStock>> SearchAsync(
        Guid organizationId,
        Guid? storageId,
        Guid? productId,
        bool onlyPositive,
        CancellationToken cancellationToken);

    Task<List<ProductStock>> GetByStorageIdAsync(
        Guid organizationId,
        Guid storageId,
        CancellationToken cancellationToken);
}

