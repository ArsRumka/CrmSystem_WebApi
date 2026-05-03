using Warehouse.Domain.Entities;

namespace Warehouse.Application.Abstractions.Repositories;

public interface IStorageRepository
{
    Task AddAsync(Storage storage, CancellationToken cancellationToken);

    Task<Storage?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<Storage>> SearchAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<bool> AnyAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<Storage?> GetDefaultAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<List<Storage>> GetActiveAsync(Guid organizationId, CancellationToken cancellationToken);

    Task ClearDefaultFlagsAsync(
        Guid organizationId,
        Guid exceptStorageId,
        DateTime updatedAt,
        CancellationToken cancellationToken);

    Task<bool> HasPositiveStockAsync(
        Guid organizationId,
        Guid storageId,
        CancellationToken cancellationToken);
}

