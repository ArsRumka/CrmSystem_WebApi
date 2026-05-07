using Bonus.Application.Abstractions.Services;
using Bonus.Application.Common;
using Bonus.Domain.Entities;
using Bonus.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bonus.Infrastructure.Services;

internal sealed class BonusDealCompletionService : IBonusDealCompletionService
{
    private const string WriteOffReason = "Bonus write-off for deal";
    private const string AccrualReason = "Bonus accrual for deal";

    private readonly ApplicationDbContext _dbContext;
    private readonly ICatalogBonusRuleResolver _catalogBonusRuleResolver;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BonusDealCompletionService(
        ApplicationDbContext dbContext,
        ICatalogBonusRuleResolver catalogBonusRuleResolver,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _catalogBonusRuleResolver = catalogBonusRuleResolver;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task CompleteDealAsync(
        Guid organizationId,
        Guid dealId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.Set<BonusTransaction>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.DealId == dealId &&
                     (x.Type == BonusTransactionType.WriteOff ||
                      x.Type == BonusTransactionType.Accrual),
                cancellationToken))
        {
            throw new ConflictException("Deal bonuses were already processed");
        }

        var deal = await _dbContext.Set<Deal>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == dealId,
                cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var settings = await _dbContext.Set<BonusSettings>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, cancellationToken);

        if (settings is null || !settings.IsEnabled)
        {
            if (deal.BonusPointsUsed > 0)
            {
                throw new ConflictException("Bonuses are disabled");
            }

            return;
        }

        var now = _dateTimeProvider.UtcNow;
        var account = await _dbContext.Set<BonusAccount>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.ClientId == deal.ClientId,
                cancellationToken);

        if (deal.BonusPointsUsed > 0)
        {
            if (account is null || account.Balance < deal.BonusPointsUsed)
            {
                throw new ConflictException("Insufficient bonus balance");
            }

            await ProcessWriteOffAsync(organizationId, deal, account, userId, now, cancellationToken);
        }

        if (deal.FinalAmount <= 0)
        {
            return;
        }

        if (deal.BonusPointsUsed > 0 && !settings.AccrueOnBonusPayment)
        {
            return;
        }

        account ??= await CreateAccountAsync(organizationId, deal.ClientId, now, cancellationToken);

        var accruedPoints = await CalculateAccrualPointsAsync(deal, settings, organizationId, cancellationToken);
        if (accruedPoints <= 0)
        {
            return;
        }

        var balanceBefore = account.Balance;
        account.Increase(accruedPoints, now);

        await _dbContext.Set<BonusTransaction>().AddAsync(
            new BonusTransaction(
                Guid.NewGuid(),
                organizationId,
                account.Id,
                account.ClientId,
                deal.Id,
                BonusTransactionType.Accrual,
                accruedPoints,
                BonusRounding.RoundMoney(accruedPoints * settings.PointValue),
                settings.PointValue,
                balanceBefore,
                account.Balance,
                AccrualReason,
                now,
                userId),
            cancellationToken);
    }

    private async Task ProcessWriteOffAsync(
        Guid organizationId,
        Deal deal,
        BonusAccount account,
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var balanceBefore = account.Balance;
        account.Decrease(deal.BonusPointsUsed, now);

        var pointValueAtMoment = BonusRounding.RoundMoney(deal.BonusDiscountAmount / deal.BonusPointsUsed);

        await _dbContext.Set<BonusTransaction>().AddAsync(
            new BonusTransaction(
                Guid.NewGuid(),
                organizationId,
                account.Id,
                account.ClientId,
                deal.Id,
                BonusTransactionType.WriteOff,
                deal.BonusPointsUsed,
                deal.BonusDiscountAmount,
                pointValueAtMoment,
                balanceBefore,
                account.Balance,
                WriteOffReason,
                now,
                userId),
            cancellationToken);
    }

    private async Task<BonusAccount> CreateAccountAsync(
        Guid organizationId,
        Guid clientId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var account = new BonusAccount(Guid.NewGuid(), organizationId, clientId, now);
        await _dbContext.Set<BonusAccount>().AddAsync(account, cancellationToken);
        return account;
    }

    private async Task<decimal> CalculateAccrualPointsAsync(
        Deal deal,
        BonusSettings settings,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var items = deal.Items.ToList();
        if (items.Count == 0)
        {
            return 0;
        }

        var preBonusTotal = BonusRounding.RoundMoney(items.Sum(x => x.FinalAmount));
        if (preBonusTotal <= 0)
        {
            return 0;
        }

        var totalBonusDiscount = deal.BonusDiscountAmount;
        var remainingBonusDiscount = deal.BonusDiscountAmount;
        var totalPoints = 0m;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var discountShare = CalculateDiscountShare(
                item.FinalAmount,
                preBonusTotal,
                totalBonusDiscount,
                remainingBonusDiscount,
                index == items.Count - 1);

            remainingBonusDiscount = BonusRounding.RoundMoney(remainingBonusDiscount - discountShare);

            var itemPaidBase = BonusRounding.RoundMoney(item.FinalAmount - discountShare);
            if (itemPaidBase <= 0)
            {
                continue;
            }

            var rule = await _catalogBonusRuleResolver.ResolveAsync(
                organizationId,
                item.ItemType,
                item.ItemId,
                settings,
                cancellationToken);

            totalPoints += CalculateItemAccrualPoints(item, itemPaidBase, rule, settings.PointValue);
        }

        return BonusRounding.RoundPoints(totalPoints);
    }

    private static decimal CalculateDiscountShare(
        decimal itemFinalAmount,
        decimal preBonusTotal,
        decimal totalBonusDiscount,
        decimal remainingBonusDiscount,
        bool isLastItem)
    {
        if (remainingBonusDiscount <= 0)
        {
            return 0;
        }

        if (isLastItem)
        {
            return Math.Min(itemFinalAmount, remainingBonusDiscount);
        }

        var share = BonusRounding.RoundMoney(totalBonusDiscount * itemFinalAmount / preBonusTotal);
        return Math.Min(itemFinalAmount, Math.Min(remainingBonusDiscount, share));
    }

    private static decimal CalculateItemAccrualPoints(
        DealItem item,
        decimal itemPaidBase,
        ResolvedBonusRule rule,
        decimal pointValue)
    {
        var points = rule.AccrualType switch
        {
            BonusAccrualType.None => 0,
            BonusAccrualType.Percent => itemPaidBase * rule.AccrualValue / 100 / pointValue,
            BonusAccrualType.Fixed => CalculateFixedPoints(item, itemPaidBase, rule.AccrualValue),
            _ => 0
        };

        return BonusRounding.RoundPoints(points);
    }

    private static decimal CalculateFixedPoints(DealItem item, decimal itemPaidBase, decimal accrualValue)
    {
        if (item.FinalAmount <= 0)
        {
            return 0;
        }

        var basePoints = accrualValue * item.Quantity;
        var paidRatio = itemPaidBase / item.FinalAmount;
        return basePoints * paidRatio;
    }
}
