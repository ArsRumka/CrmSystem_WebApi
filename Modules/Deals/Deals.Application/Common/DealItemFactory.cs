using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Lookups;
using Deals.Application.Contracts;
using Deals.Domain.Entities;
using Deals.Domain.Enums;

namespace Deals.Application.Common;

internal sealed record BuiltDealItems(
    List<DealItem> Items,
    List<DealItemCalculation> Calculations);

internal static class DealItemFactory
{
    public static async Task<BuiltDealItems> BuildAsync(
        Guid organizationId,
        Guid dealId,
        IReadOnlyCollection<DealItemRequest> itemRequests,
        ICatalogLookupService catalogLookupService,
        DealCalculationService calculationService,
        CancellationToken cancellationToken)
    {
        var items = new List<DealItem>();
        var calculations = new List<DealItemCalculation>();

        foreach (var request in itemRequests)
        {
            var snapshot = await catalogLookupService.GetItemSnapshotAsync(
                organizationId,
                request.ItemType,
                request.ItemId,
                cancellationToken);

            if (snapshot is null)
            {
                throw new NotFoundException("Catalog item was not found");
            }

            if (!snapshot.IsActive)
            {
                throw new ConflictException("Catalog item is inactive");
            }

            var discountType = request.ManualDiscountType ?? snapshot.DiscountType;
            var discountValue = ResolveDiscountValue(discountType, request.ManualDiscountType.HasValue
                ? request.ManualDiscountValue
                : snapshot.DiscountValue);

            var calculation = calculationService.CalculateItem(
                request.Quantity,
                snapshot.Price,
                discountType,
                discountValue);

            calculations.Add(calculation);
            items.Add(new DealItem(
                Guid.NewGuid(),
                organizationId,
                dealId,
                request.ItemType,
                request.ItemId,
                request.StorageId,
                snapshot.Name,
                request.Quantity,
                snapshot.Price,
                discountType,
                discountValue,
                calculation.DiscountAmount,
                calculation.TotalAmount,
                calculation.FinalAmount));
        }

        return new BuiltDealItems(items, calculations);
    }

    private static decimal? ResolveDiscountValue(DealDiscountType discountType, decimal? discountValue)
    {
        return discountType == DealDiscountType.None ? null : discountValue;
    }
}
