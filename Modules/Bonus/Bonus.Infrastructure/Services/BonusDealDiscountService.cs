using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Abstractions.Services;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using BuildingBlocks.Application.Exceptions;

namespace Bonus.Infrastructure.Services;

public sealed class BonusDealDiscountService : IBonusDealDiscountService
{
    private readonly IBonusSettingsRepository _settingsRepository;
    private readonly IBonusAccountRepository _accountRepository;

    public BonusDealDiscountService(
        IBonusSettingsRepository settingsRepository,
        IBonusAccountRepository accountRepository)
    {
        _settingsRepository = settingsRepository;
        _accountRepository = accountRepository;
    }

    public async Task<DealBonusDiscountResult> CalculateAsync(
        Guid organizationId,
        Guid clientId,
        decimal amountBeforeBonus,
        decimal requestedBonusPoints,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required", nameof(clientId));

        if (amountBeforeBonus < 0)
            throw new ArgumentException("AmountBeforeBonus must be greater than or equal to zero", nameof(amountBeforeBonus));

        if (requestedBonusPoints < 0)
            throw new ArgumentException("RequestedBonusPoints must be greater than or equal to zero", nameof(requestedBonusPoints));

        var requestedPoints = BonusRounding.RoundPoints(requestedBonusPoints);
        if (requestedPoints == 0)
        {
            return new DealBonusDiscountResult(0, 0);
        }

        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        if (settings is null || !settings.IsEnabled)
        {
            throw new ConflictException("Bonuses are disabled");
        }

        if (settings.MaxPaymentPercent <= 0)
        {
            throw new ConflictException("Bonus payment is disabled");
        }

        var account = await _accountRepository.GetByClientIdAsync(organizationId, clientId, cancellationToken);
        if (account is null || account.Balance <= 0)
        {
            throw new ConflictException("Bonus balance is zero");
        }

        var roundedAmountBeforeBonus = BonusRounding.RoundMoney(amountBeforeBonus);
        var maxBonusDiscount = BonusRounding.RoundMoney(roundedAmountBeforeBonus * settings.MaxPaymentPercent / 100);
        var requestedDiscount = BonusRounding.RoundMoney(requestedPoints * settings.PointValue);
        var balanceDiscount = BonusRounding.RoundMoney(account.Balance * settings.PointValue);

        var discount = new[]
            {
                requestedDiscount,
                maxBonusDiscount,
                roundedAmountBeforeBonus,
                balanceDiscount
            }
            .Min();

        if (discount <= 0)
        {
            return new DealBonusDiscountResult(0, 0);
        }

        var appliedPoints = BonusRounding.RoundPoints(discount / settings.PointValue);
        appliedPoints = Math.Min(appliedPoints, requestedPoints);
        appliedPoints = Math.Min(appliedPoints, account.Balance);
        appliedPoints = BonusRounding.RoundPoints(appliedPoints);

        var bonusDiscountAmount = BonusRounding.RoundMoney(appliedPoints * settings.PointValue);
        if (bonusDiscountAmount > roundedAmountBeforeBonus)
        {
            bonusDiscountAmount = roundedAmountBeforeBonus;
            appliedPoints = BonusRounding.RoundPoints(bonusDiscountAmount / settings.PointValue);
        }

        if (bonusDiscountAmount > maxBonusDiscount)
        {
            bonusDiscountAmount = maxBonusDiscount;
            appliedPoints = BonusRounding.RoundPoints(bonusDiscountAmount / settings.PointValue);
        }

        return new DealBonusDiscountResult(appliedPoints, bonusDiscountAmount);
    }
}
