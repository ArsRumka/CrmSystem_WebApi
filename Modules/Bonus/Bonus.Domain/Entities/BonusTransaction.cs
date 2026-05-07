using Bonus.Domain.Enums;

namespace Bonus.Domain.Entities;

public class BonusTransaction
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BonusAccountId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? DealId { get; private set; }
    public Guid? SourceReturnId { get; private set; }
    public BonusTransactionType Type { get; private set; }
    public decimal Points { get; private set; }
    public decimal MonetaryAmount { get; private set; }
    public decimal PointValueAtMoment { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    private BonusTransaction()
    {
    }

    public BonusTransaction(
        Guid id,
        Guid organizationId,
        Guid bonusAccountId,
        Guid clientId,
        Guid? dealId,
        BonusTransactionType type,
        decimal points,
        decimal monetaryAmount,
        decimal pointValueAtMoment,
        decimal balanceBefore,
        decimal balanceAfter,
        string? reason,
        DateTime createdAt,
        Guid? createdByUserId,
        Guid? sourceReturnId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (bonusAccountId == Guid.Empty)
            throw new ArgumentException("BonusAccountId is required", nameof(bonusAccountId));

        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required", nameof(clientId));

        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty", nameof(dealId));

        if (sourceReturnId == Guid.Empty)
            throw new ArgumentException("SourceReturnId cannot be empty", nameof(sourceReturnId));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId cannot be empty", nameof(createdByUserId));

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid bonus transaction type", nameof(type));

        if (points <= 0)
            throw new ArgumentException("Points must be greater than zero", nameof(points));

        if (monetaryAmount < 0)
            throw new ArgumentException("MonetaryAmount must be greater than or equal to zero", nameof(monetaryAmount));

        if (pointValueAtMoment <= 0)
            throw new ArgumentException("PointValueAtMoment must be greater than zero", nameof(pointValueAtMoment));

        if (balanceBefore < 0)
            throw new ArgumentException("BalanceBefore must be greater than or equal to zero", nameof(balanceBefore));

        if (balanceAfter < 0)
            throw new ArgumentException("BalanceAfter must be greater than or equal to zero", nameof(balanceAfter));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        BonusAccountId = bonusAccountId;
        ClientId = clientId;
        DealId = dealId;
        SourceReturnId = sourceReturnId;
        Type = type;
        Points = points;
        MonetaryAmount = monetaryAmount;
        PointValueAtMoment = pointValueAtMoment;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
        Reason = NormalizeReason(reason, type);
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId;
    }

    private static string? NormalizeReason(string? reason, BonusTransactionType type)
    {
        var requiresReason = type is BonusTransactionType.CorrectionIncrease or BonusTransactionType.CorrectionDecrease;

        if (string.IsNullOrWhiteSpace(reason))
        {
            if (requiresReason)
                throw new ArgumentException("Reason is required", nameof(reason));

            return null;
        }

        var normalized = reason.Trim();
        if (normalized.Length > 1000)
            throw new ArgumentException("Reason cannot exceed 1000 characters", nameof(reason));

        return normalized;
    }
}
