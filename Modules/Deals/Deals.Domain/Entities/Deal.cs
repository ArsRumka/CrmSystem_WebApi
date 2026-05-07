namespace Deals.Domain.Entities;

public class Deal
{
    private readonly List<DealItem> _items = [];
    private readonly List<DealStageHistory> _stageHistory = [];

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid ResponsibleUserId { get; private set; }
    public Guid StageId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal BonusPointsUsed { get; private set; }
    public decimal BonusDiscountAmount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyCollection<DealItem> Items => _items;
    public IReadOnlyCollection<DealStageHistory> StageHistory => _stageHistory;

    private Deal()
    {
    }

    public Deal(
        Guid id,
        Guid organizationId,
        Guid clientId,
        Guid responsibleUserId,
        Guid stageId,
        decimal totalAmount,
        decimal discountAmount,
        decimal bonusPointsUsed,
        decimal bonusDiscountAmount,
        decimal finalAmount,
        string? notes,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required", nameof(clientId));

        if (responsibleUserId == Guid.Empty)
            throw new ArgumentException("ResponsibleUserId is required", nameof(responsibleUserId));

        if (stageId == Guid.Empty)
            throw new ArgumentException("StageId is required", nameof(stageId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        ValidateAmounts(totalAmount, discountAmount, bonusPointsUsed, bonusDiscountAmount, finalAmount);

        Id = id;
        OrganizationId = organizationId;
        ClientId = clientId;
        ResponsibleUserId = responsibleUserId;
        StageId = stageId;
        TotalAmount = totalAmount;
        DiscountAmount = discountAmount;
        BonusPointsUsed = bonusPointsUsed;
        BonusDiscountAmount = bonusDiscountAmount;
        FinalAmount = finalAmount;
        IsActive = true;
        CreatedAt = createdAt;
        Notes = NormalizeOptional(notes);
    }

    public void UpdateDetails(
        Guid clientId,
        Guid responsibleUserId,
        decimal totalAmount,
        decimal discountAmount,
        decimal bonusPointsUsed,
        decimal bonusDiscountAmount,
        decimal finalAmount,
        string? notes,
        DateTime updatedAt)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required", nameof(clientId));

        if (responsibleUserId == Guid.Empty)
            throw new ArgumentException("ResponsibleUserId is required", nameof(responsibleUserId));

        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        ValidateAmounts(totalAmount, discountAmount, bonusPointsUsed, bonusDiscountAmount, finalAmount);

        ClientId = clientId;
        ResponsibleUserId = responsibleUserId;
        TotalAmount = totalAmount;
        DiscountAmount = discountAmount;
        BonusPointsUsed = bonusPointsUsed;
        BonusDiscountAmount = bonusDiscountAmount;
        FinalAmount = finalAmount;
        Notes = NormalizeOptional(notes);
        UpdatedAt = updatedAt;
    }

    public void ReplaceItems(IEnumerable<DealItem> items)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
            throw new ArgumentException("Deal must contain at least one item", nameof(items));

        if (itemList.Any(x => x.OrganizationId != OrganizationId || x.DealId != Id))
            throw new ArgumentException("All deal items must belong to the deal", nameof(items));

        _items.Clear();
        _items.AddRange(itemList);
    }

    public void AddStageHistory(DealStageHistory history)
    {
        if (history.OrganizationId != OrganizationId || history.DealId != Id)
            throw new ArgumentException("Stage history must belong to the deal", nameof(history));

        _stageHistory.Add(history);
    }

    public void ChangeStage(Guid newStageId, bool isFinal, DateTime updatedAt)
    {
        if (newStageId == Guid.Empty)
            throw new ArgumentException("StageId is required", nameof(newStageId));

        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        StageId = newStageId;
        UpdatedAt = updatedAt;

        if (isFinal)
        {
            ClosedAt = updatedAt;
        }
    }

    public void Deactivate(DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        IsActive = false;
        UpdatedAt = updatedAt;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateAmounts(
        decimal totalAmount,
        decimal discountAmount,
        decimal bonusPointsUsed,
        decimal bonusDiscountAmount,
        decimal finalAmount)
    {
        if (totalAmount < 0)
            throw new ArgumentException("TotalAmount must be greater than or equal to zero", nameof(totalAmount));

        if (discountAmount < 0)
            throw new ArgumentException("DiscountAmount must be greater than or equal to zero", nameof(discountAmount));

        if (bonusPointsUsed < 0)
            throw new ArgumentException("BonusPointsUsed must be greater than or equal to zero", nameof(bonusPointsUsed));

        if (bonusDiscountAmount < 0)
            throw new ArgumentException("BonusDiscountAmount must be greater than or equal to zero", nameof(bonusDiscountAmount));

        if (finalAmount < 0)
            throw new ArgumentException("FinalAmount must be greater than or equal to zero", nameof(finalAmount));

        if (discountAmount > totalAmount)
            throw new ArgumentException("DiscountAmount cannot exceed TotalAmount", nameof(discountAmount));

        if (bonusDiscountAmount > totalAmount - discountAmount)
            throw new ArgumentException("BonusDiscountAmount cannot exceed remaining amount", nameof(bonusDiscountAmount));

        if (finalAmount != totalAmount - discountAmount - bonusDiscountAmount)
            throw new ArgumentException("FinalAmount must equal TotalAmount minus discounts", nameof(finalAmount));
    }
}
