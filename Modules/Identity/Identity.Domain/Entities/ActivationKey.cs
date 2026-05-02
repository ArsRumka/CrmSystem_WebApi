using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class ActivationKey : Entity
{
    public string KeyHash { get; private set; } = null!;
    public Guid OrganizationRequestId { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBySystemAdminId { get; private set; }

    private ActivationKey() : base(Guid.Empty) { }

    public ActivationKey(
        Guid id,
        string keyHash,
        Guid organizationRequestId,
        DateTime? expiresAt,
        DateTime createdAt,
        Guid createdBySystemAdminId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new ArgumentException("Key hash is required");

        if (organizationRequestId == Guid.Empty)
            throw new ArgumentException("Organization request id is required");

        if (createdBySystemAdminId == Guid.Empty)
            throw new ArgumentException("System admin id is required");

        KeyHash = keyHash;
        OrganizationRequestId = organizationRequestId;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CreatedBySystemAdminId = createdBySystemAdminId;
        IsUsed = false;
    }

    public bool IsExpired(DateTime utcNow)
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= utcNow;
    }

    public void MarkAsUsed(DateTime usedAt)
    {
        if (IsUsed)
            throw new InvalidOperationException("Activation key is already used");

        IsUsed = true;
        UsedAt = usedAt;
    }
}
