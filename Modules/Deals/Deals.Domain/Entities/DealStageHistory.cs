namespace Deals.Domain.Entities;

public class DealStageHistory
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid DealId { get; private set; }
    public Guid? OldStageId { get; private set; }
    public Guid NewStageId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private DealStageHistory()
    {
    }

    public DealStageHistory(
        Guid id,
        Guid organizationId,
        Guid dealId,
        Guid? oldStageId,
        Guid newStageId,
        Guid changedByUserId,
        DateTime changedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId is required", nameof(dealId));

        if (oldStageId == Guid.Empty)
            throw new ArgumentException("OldStageId cannot be empty", nameof(oldStageId));

        if (newStageId == Guid.Empty)
            throw new ArgumentException("NewStageId is required", nameof(newStageId));

        if (changedByUserId == Guid.Empty)
            throw new ArgumentException("ChangedByUserId is required", nameof(changedByUserId));

        if (changedAt == default)
            throw new ArgumentException("ChangedAt is required", nameof(changedAt));

        Id = id;
        OrganizationId = organizationId;
        DealId = dealId;
        OldStageId = oldStageId;
        NewStageId = newStageId;
        ChangedByUserId = changedByUserId;
        ChangedAt = changedAt;
    }
}
