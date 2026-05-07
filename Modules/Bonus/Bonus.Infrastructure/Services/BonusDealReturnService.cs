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

internal sealed class BonusDealReturnService : IBonusDealReturnService
{
    private const string RefundReasonPrefix = "Возврат списанных бонусов: ";
    private const string AccrualReversalReasonPrefix = "Отмена начисления по возврату: ";

    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BonusDealReturnService(
        ApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<BonusDealReturnResult> ProcessReturnAsync(
        Guid organizationId,
        Guid dealId,
        Guid dealReturnId,
        Guid userId,
        decimal returnRatio,
        decimal returnAmount,
        decimal bonusDiscountMoneyShare,
        string reason,
        CancellationToken cancellationToken)
    {
        if (returnRatio < 0)
            throw new ArgumentException("ReturnRatio must be greater than or equal to zero", nameof(returnRatio));

        if (returnAmount < 0)
            throw new ArgumentException("ReturnAmount must be greater than or equal to zero", nameof(returnAmount));

        if (bonusDiscountMoneyShare < 0)
            throw new ArgumentException("BonusDiscountMoneyShare must be greater than or equal to zero", nameof(bonusDiscountMoneyShare));

        if (await _dbContext.Set<BonusTransaction>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.SourceReturnId == dealReturnId &&
                     (x.Type == BonusTransactionType.Refund ||
                      x.Type == BonusTransactionType.CorrectionDecrease),
                cancellationToken))
        {
            throw new ConflictException("Deal return bonuses were already processed");
        }

        var deal = await _dbContext.Set<Deal>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == dealId,
                cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var transactions = await _dbContext.Set<BonusTransaction>()
            .Where(x => x.OrganizationId == organizationId && x.DealId == dealId)
            .ToListAsync(cancellationToken);

        var account = await _dbContext.Set<BonusAccount>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.ClientId == deal.ClientId,
                cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var bonusPointsReturned = await ProcessWriteOffRefundAsync(
            organizationId,
            deal,
            dealReturnId,
            userId,
            returnRatio,
            bonusDiscountMoneyShare,
            reason,
            transactions,
            account,
            now,
            cancellationToken);

        if (bonusPointsReturned.Account is not null)
        {
            account = bonusPointsReturned.Account;
        }

        var bonusAccrualReversed = await ProcessAccrualReversalAsync(
            deal,
            dealReturnId,
            userId,
            returnRatio,
            reason,
            transactions,
            account,
            now,
            cancellationToken);

        return new BonusDealReturnResult(
            bonusPointsReturned.Points,
            bonusAccrualReversed);
    }

    private async Task<(decimal Points, BonusAccount? Account)> ProcessWriteOffRefundAsync(
        Guid organizationId,
        Deal deal,
        Guid dealReturnId,
        Guid userId,
        decimal returnRatio,
        decimal bonusDiscountMoneyShare,
        string reason,
        IReadOnlyCollection<BonusTransaction> transactions,
        BonusAccount? account,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var writeOffTransactions = transactions
            .Where(x => x.Type == BonusTransactionType.WriteOff)
            .ToList();

        var originalWriteOffPoints = BonusRounding.RoundPoints(writeOffTransactions.Sum(x => x.Points));
        if (originalWriteOffPoints <= 0 || returnRatio <= 0)
        {
            return (0, account);
        }

        var alreadyRefundedPoints = BonusRounding.RoundPoints(transactions
            .Where(x => x.SourceReturnId.HasValue && x.Type == BonusTransactionType.Refund)
            .Sum(x => x.Points));

        var remainingPoints = BonusRounding.RoundPoints(originalWriteOffPoints - alreadyRefundedPoints);
        if (remainingPoints <= 0)
        {
            return (0, account);
        }

        var requestedPoints = BonusRounding.RoundPoints(deal.BonusPointsUsed * returnRatio);
        var pointsToReturn = Math.Min(requestedPoints, remainingPoints);
        pointsToReturn = BonusRounding.RoundPoints(pointsToReturn);

        if (pointsToReturn <= 0)
        {
            return (0, account);
        }

        account ??= await CreateAccountAsync(organizationId, deal.ClientId, now, cancellationToken);

        var pointValueAtMoment = ResolvePointValue(writeOffTransactions);
        var monetaryAmount = BonusRounding.RoundMoney(pointsToReturn * pointValueAtMoment);

        var balanceBefore = account.Balance;
        account.Increase(pointsToReturn, now);

        await _dbContext.Set<BonusTransaction>().AddAsync(
            new BonusTransaction(
                Guid.NewGuid(),
                deal.OrganizationId,
                account.Id,
                account.ClientId,
                deal.Id,
                BonusTransactionType.Refund,
                pointsToReturn,
                monetaryAmount,
                pointValueAtMoment,
                balanceBefore,
                account.Balance,
                BuildReason(RefundReasonPrefix, reason),
                now,
                userId,
                dealReturnId),
            cancellationToken);

        return (pointsToReturn, account);
    }

    private async Task<decimal> ProcessAccrualReversalAsync(
        Deal deal,
        Guid dealReturnId,
        Guid userId,
        decimal returnRatio,
        string reason,
        IReadOnlyCollection<BonusTransaction> transactions,
        BonusAccount? account,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var accrualTransactions = transactions
            .Where(x => x.Type == BonusTransactionType.Accrual)
            .ToList();

        var originalAccrualPoints = BonusRounding.RoundPoints(accrualTransactions.Sum(x => x.Points));
        if (originalAccrualPoints <= 0 || returnRatio <= 0)
        {
            return 0;
        }

        var alreadyReversedPoints = BonusRounding.RoundPoints(transactions
            .Where(x => x.SourceReturnId.HasValue && x.Type == BonusTransactionType.CorrectionDecrease)
            .Sum(x => x.Points));

        var remainingPoints = BonusRounding.RoundPoints(originalAccrualPoints - alreadyReversedPoints);
        if (remainingPoints <= 0)
        {
            return 0;
        }

        var requestedPoints = BonusRounding.RoundPoints(originalAccrualPoints * returnRatio);
        var pointsToReverse = Math.Min(requestedPoints, remainingPoints);
        pointsToReverse = BonusRounding.RoundPoints(pointsToReverse);

        if (pointsToReverse <= 0)
        {
            return 0;
        }

        if (account is null || account.Balance < pointsToReverse)
        {
            throw new ConflictException("Insufficient bonus balance to reverse accrual");
        }

        var pointValueAtMoment = ResolvePointValue(accrualTransactions);
        var balanceBefore = account.Balance;
        account.Decrease(pointsToReverse, now);

        await _dbContext.Set<BonusTransaction>().AddAsync(
            new BonusTransaction(
                Guid.NewGuid(),
                deal.OrganizationId,
                account.Id,
                account.ClientId,
                deal.Id,
                BonusTransactionType.CorrectionDecrease,
                pointsToReverse,
                BonusRounding.RoundMoney(pointsToReverse * pointValueAtMoment),
                pointValueAtMoment,
                balanceBefore,
                account.Balance,
                BuildReason(AccrualReversalReasonPrefix, reason),
                now,
                userId,
                dealReturnId),
            cancellationToken);

        return pointsToReverse;
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

    private static decimal ResolvePointValue(IReadOnlyCollection<BonusTransaction> transactions)
    {
        var points = transactions.Sum(x => x.Points);
        if (points <= 0)
        {
            return 1.00m;
        }

        var monetaryAmount = transactions.Sum(x => x.MonetaryAmount);
        if (monetaryAmount <= 0)
        {
            return transactions.First().PointValueAtMoment;
        }

        return BonusRounding.RoundMoney(monetaryAmount / points);
    }

    private static string BuildReason(string prefix, string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Возврат сделки" : reason.Trim();
        var availableLength = Math.Max(0, 1000 - prefix.Length);
        if (normalizedReason.Length > availableLength)
        {
            normalizedReason = normalizedReason[..availableLength];
        }

        return prefix + normalizedReason;
    }
}
