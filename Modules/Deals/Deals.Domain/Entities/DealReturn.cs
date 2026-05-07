using Deals.Domain.Enums;

namespace Deals.Domain.Entities;

public class DealReturn
{
    private readonly List<DealReturnItem> _items = [];

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid DealId { get; private set; }
    public DealReturnStatus Status { get; private set; }
    public string Reason { get; private set; } = null!;
    public string? CancellationReason { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal BonusPointsReturned { get; private set; }
    public decimal BonusAccrualReversed { get; private set; }
    public decimal MoneyAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<DealReturnItem> Items => _items;

    private DealReturn()
    {
    }

    public DealReturn(
        Guid id,
        Guid organizationId,
        Guid dealId,
        string reason,
        decimal totalAmount,
        decimal moneyAmount,
        Guid createdByUserId,
        DateTime createdAt,
        IEnumerable<DealReturnItem> items)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId is required", nameof(dealId));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required", nameof(createdByUserId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        ValidateAmounts(totalAmount, bonusPointsReturned: 0, bonusAccrualReversed: 0, moneyAmount);

        Id = id;
        OrganizationId = organizationId;
        DealId = dealId;
        Status = DealReturnStatus.Draft;
        Reason = Require(reason, nameof(reason), 1000);
        TotalAmount = totalAmount;
        MoneyAmount = moneyAmount;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;

        ReplaceItems(items);
    }

    public void Update(
        string reason,
        decimal totalAmount,
        decimal moneyAmount,
        DateTime updatedAt,
        IEnumerable<DealReturnItem> items)
    {
        EnsureDraft();

        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        ValidateAmounts(totalAmount, bonusPointsReturned: 0, bonusAccrualReversed: 0, moneyAmount);

        Reason = Require(reason, nameof(reason), 1000);
        TotalAmount = totalAmount;
        BonusPointsReturned = 0;
        BonusAccrualReversed = 0;
        MoneyAmount = moneyAmount;
        UpdatedAt = updatedAt;

        ReplaceItems(items);
    }

    public void Complete(
        decimal totalAmount,
        decimal bonusPointsReturned,
        decimal bonusAccrualReversed,
        decimal moneyAmount,
        Guid completedByUserId,
        DateTime completedAt)
    {
        EnsureDraft();

        if (completedByUserId == Guid.Empty)
            throw new ArgumentException("CompletedByUserId is required", nameof(completedByUserId));

        if (completedAt == default)
            throw new ArgumentException("CompletedAt is required", nameof(completedAt));

        ValidateAmounts(totalAmount, bonusPointsReturned, bonusAccrualReversed, moneyAmount);

        TotalAmount = totalAmount;
        BonusPointsReturned = bonusPointsReturned;
        BonusAccrualReversed = bonusAccrualReversed;
        MoneyAmount = moneyAmount;
        Status = DealReturnStatus.Completed;
        CompletedAt = completedAt;
        CompletedByUserId = completedByUserId;
    }

    public void Cancel(string cancellationReason, Guid cancelledByUserId, DateTime cancelledAt)
    {
        EnsureDraft();

        if (cancelledByUserId == Guid.Empty)
            throw new ArgumentException("CancelledByUserId is required", nameof(cancelledByUserId));

        if (cancelledAt == default)
            throw new ArgumentException("CancelledAt is required", nameof(cancelledAt));

        Status = DealReturnStatus.Cancelled;
        CancellationReason = Require(cancellationReason, nameof(cancellationReason), 1000);
        CancelledAt = cancelledAt;
        CancelledByUserId = cancelledByUserId;
    }

    private void ReplaceItems(IEnumerable<DealReturnItem> items)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
            throw new ArgumentException("Deal return must contain at least one item", nameof(items));

        if (itemList.Any(x =>
                x.OrganizationId != OrganizationId ||
                x.DealReturnId != Id ||
                x.DealId != DealId))
        {
            throw new ArgumentException("All return items must belong to the return", nameof(items));
        }

        _items.Clear();
        _items.AddRange(itemList);
    }

    private void EnsureDraft()
    {
        if (Status != DealReturnStatus.Draft)
            throw new InvalidOperationException("Only draft returns can be changed");
    }

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }

    private static void ValidateAmounts(
        decimal totalAmount,
        decimal bonusPointsReturned,
        decimal bonusAccrualReversed,
        decimal moneyAmount)
    {
        if (totalAmount < 0)
            throw new ArgumentException("TotalAmount must be greater than or equal to zero", nameof(totalAmount));

        if (bonusPointsReturned < 0)
            throw new ArgumentException("BonusPointsReturned must be greater than or equal to zero", nameof(bonusPointsReturned));

        if (bonusAccrualReversed < 0)
            throw new ArgumentException("BonusAccrualReversed must be greater than or equal to zero", nameof(bonusAccrualReversed));

        if (moneyAmount < 0)
            throw new ArgumentException("MoneyAmount must be greater than or equal to zero", nameof(moneyAmount));
    }
}
