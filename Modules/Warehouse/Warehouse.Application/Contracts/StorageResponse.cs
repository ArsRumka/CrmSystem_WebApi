using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts;

public sealed record StorageResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Address,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class StorageResponseMapper
{
    public static StorageResponse ToResponse(this Storage storage)
    {
        return new StorageResponse(
            storage.Id,
            storage.OrganizationId,
            storage.Name,
            storage.Address,
            storage.IsDefault,
            storage.IsActive,
            storage.CreatedAt,
            storage.UpdatedAt);
    }
}

