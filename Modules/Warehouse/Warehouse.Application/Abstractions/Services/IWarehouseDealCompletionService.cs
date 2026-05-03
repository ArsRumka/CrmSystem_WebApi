namespace Warehouse.Application.Abstractions.Services;

public interface IWarehouseDealCompletionService
{
    Task CompleteDealAsync(
        Guid organizationId,
        Guid dealId,
        Guid userId,
        CancellationToken cancellationToken);
}

