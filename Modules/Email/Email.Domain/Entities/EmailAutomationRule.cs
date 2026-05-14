namespace Email.Domain.Entities;

public class EmailAutomationRule
{
    public const int DefaultInactivityDays = 60;
    public const int DefaultRepeatAfterDays = 30;

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public bool IsEnabled { get; private set; }
    public int InactivityDays { get; private set; }
    public int RepeatAfterDays { get; private set; }
    public DateTime? LastRunAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private EmailAutomationRule()
    {
    }

    public EmailAutomationRule(
        Guid id,
        Guid organizationId,
        bool isEnabled,
        Guid? templateId,
        int inactivityDays,
        int repeatAfterDays,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        CreatedAt = createdAt;

        SetValues(isEnabled, templateId, inactivityDays, repeatAfterDays);
    }

    public static EmailAutomationRule CreateDefault(Guid organizationId, DateTime createdAt)
    {
        return new EmailAutomationRule(
            Guid.NewGuid(),
            organizationId,
            isEnabled: false,
            templateId: null,
            DefaultInactivityDays,
            DefaultRepeatAfterDays,
            createdAt);
    }

    public void Update(
        bool isEnabled,
        Guid? templateId,
        int inactivityDays,
        int repeatAfterDays,
        DateTime updatedAt,
        Guid updatedByUserId)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        if (updatedByUserId == Guid.Empty)
            throw new ArgumentException("UpdatedByUserId is required", nameof(updatedByUserId));

        SetValues(isEnabled, templateId, inactivityDays, repeatAfterDays);
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    public void MarkRun(DateTime runAt)
    {
        if (runAt == default)
            throw new ArgumentException("RunAt is required", nameof(runAt));

        LastRunAt = runAt;
    }

    private void SetValues(bool isEnabled, Guid? templateId, int inactivityDays, int repeatAfterDays)
    {
        if (templateId == Guid.Empty)
            throw new ArgumentException("TemplateId cannot be empty", nameof(templateId));

        if (isEnabled && templateId is null)
            throw new ArgumentException("TemplateId is required when automation is enabled", nameof(templateId));

        if (inactivityDays < 1)
            throw new ArgumentException("InactivityDays must be greater than or equal to 1", nameof(inactivityDays));

        if (repeatAfterDays < 1)
            throw new ArgumentException("RepeatAfterDays must be greater than or equal to 1", nameof(repeatAfterDays));

        IsEnabled = isEnabled;
        TemplateId = templateId;
        InactivityDays = inactivityDays;
        RepeatAfterDays = repeatAfterDays;
    }
}
