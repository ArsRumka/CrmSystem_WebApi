using BuildingBlocks.Application.Exceptions;
using Deals.Application.Contracts;
using Deals.Domain.Entities;
using Deals.Domain.Enums;

namespace Deals.Application.Common;

public sealed record DealReturnCalculation(
    IReadOnlyList<DealReturnItem> Items,
    decimal TotalAmount,
    decimal ReturnRatio,
    decimal BonusDiscountMoneyShare,
    decimal MoneyAmount);

public sealed class DealReturnCalculationService
{
    public DealReturnCalculation Calculate(
        Deal deal,
        Guid dealReturnId,
        IReadOnlyCollection<DealReturnItemRequest> requests,
        IReadOnlyCollection<DealReturnItem> completedItems,
        IReadOnlyCollection<DealReturn> completedReturns)
    {
        if (requests.Count == 0)
        {
            throw new ConflictException("Return must contain at least one item");
        }

        var duplicateDealItemId = requests
            .GroupBy(x => x.DealItemId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateDealItemId.HasValue)
        {
            throw new ConflictException("Duplicate deal return item");
        }

        var dealItems = deal.Items.ToDictionary(item => item.Id);
        var completedQuantities = completedItems
            .GroupBy(item => item.DealItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var returnItems = new List<DealReturnItem>();

        foreach (var request in requests)
        {
            if (!dealItems.TryGetValue(request.DealItemId, out var dealItem))
            {
                throw new NotFoundException("Deal item was not found");
            }

            if (request.Quantity <= 0)
            {
                throw new ConflictException("Returned quantity must be greater than zero");
            }

            var alreadyReturnedQuantity = completedQuantities.GetValueOrDefault(dealItem.Id);
            var remainingQuantity = dealItem.Quantity - alreadyReturnedQuantity;
            if (request.Quantity > remainingQuantity)
            {
                throw new ConflictException("Returned quantity cannot exceed sold quantity");
            }

            var storageId = ResolveStorageId(dealItem, request.StorageId);
            var returnAmount = RoundMoney(dealItem.FinalAmount * request.Quantity / dealItem.Quantity);

            returnItems.Add(new DealReturnItem(
                Guid.NewGuid(),
                deal.OrganizationId,
                dealReturnId,
                deal.Id,
                dealItem.Id,
                dealItem.ItemType,
                dealItem.ItemId,
                storageId,
                dealItem.NameSnapshot,
                RoundQuantity(request.Quantity),
                returnAmount));
        }

        var totalAmount = RoundMoney(returnItems.Sum(item => item.ReturnAmount));
        var returnRatio = CalculateReturnRatio(deal, totalAmount);
        var bonusDiscountMoneyShare = CalculateBonusDiscountMoneyShare(deal, returnRatio, completedReturns);
        var moneyAmount = Math.Max(0, RoundMoney(totalAmount - bonusDiscountMoneyShare));

        return new DealReturnCalculation(
            returnItems,
            totalAmount,
            returnRatio,
            bonusDiscountMoneyShare,
            moneyAmount);
    }

    private static Guid? ResolveStorageId(DealItem dealItem, Guid? requestedStorageId)
    {
        if (requestedStorageId == Guid.Empty)
        {
            throw new ConflictException("StorageId cannot be empty");
        }

        if (dealItem.ItemType == DealItemType.Service)
        {
            return null;
        }

        var storageId = requestedStorageId ?? dealItem.StorageId;
        if (!storageId.HasValue)
        {
            throw new ConflictException("StorageId is required for product return items");
        }

        return storageId.Value;
    }

    private static decimal CalculateReturnRatio(Deal deal, decimal totalAmount)
    {
        var amountBeforeBonus = RoundMoney(deal.TotalAmount - deal.DiscountAmount);
        if (amountBeforeBonus == 0)
        {
            if (totalAmount == 0)
            {
                return 0;
            }

            throw new ConflictException("Deal amount before bonus is zero");
        }

        return totalAmount / amountBeforeBonus;
    }

    private static decimal CalculateBonusDiscountMoneyShare(
        Deal deal,
        decimal returnRatio,
        IReadOnlyCollection<DealReturn> completedReturns)
    {
        if (deal.BonusDiscountAmount <= 0 || returnRatio <= 0)
        {
            return 0;
        }

        var alreadyReturnedShare = completedReturns
            .Sum(x => Math.Max(0, x.TotalAmount - x.MoneyAmount));

        var remainingShare = Math.Max(0, RoundMoney(deal.BonusDiscountAmount - alreadyReturnedShare));
        var requestedShare = RoundMoney(deal.BonusDiscountAmount * returnRatio);

        return Math.Min(requestedShare, remainingShare);
    }

    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundQuantity(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
