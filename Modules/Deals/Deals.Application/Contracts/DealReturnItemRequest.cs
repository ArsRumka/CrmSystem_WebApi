namespace Deals.Application.Contracts;

public sealed record DealReturnItemRequest(
    Guid DealItemId,
    decimal Quantity,
    Guid? StorageId);
