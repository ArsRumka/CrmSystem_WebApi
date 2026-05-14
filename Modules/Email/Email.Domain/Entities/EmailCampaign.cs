using Email.Domain.Enums;

namespace Email.Domain.Entities;

public class EmailCampaign
{
    private readonly List<EmailCampaignRecipient> _recipients = [];

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid TemplateId { get; private set; }
    public string Name { get; private set; } = null!;
    public EmailCampaignType Type { get; private set; }
    public EmailCampaignStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int TotalRecipients { get; private set; }
    public int SentCount { get; private set; }
    public int FailedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public IReadOnlyCollection<EmailCampaignRecipient> Recipients => _recipients;

    private EmailCampaign()
    {
    }

    public EmailCampaign(
        Guid id,
        Guid organizationId,
        Guid templateId,
        string name,
        EmailCampaignType type,
        Guid? createdByUserId,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (templateId == Guid.Empty)
            throw new ArgumentException("TemplateId is required", nameof(templateId));

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid campaign type", nameof(type));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId cannot be empty", nameof(createdByUserId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        TemplateId = templateId;
        Name = Require(name, nameof(name), 200);
        Type = type;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Status = EmailCampaignStatus.Draft;
    }

    public void AddRecipients(IEnumerable<EmailCampaignRecipient> recipients)
    {
        var recipientList = recipients.ToList();

        if (recipientList.Any(x => x.OrganizationId != OrganizationId || x.CampaignId != Id))
            throw new ArgumentException("All recipients must belong to the campaign", nameof(recipients));

        _recipients.AddRange(recipientList);
        RecalculateCounts();
    }

    public void Start(DateTime startedAt)
    {
        if (startedAt == default)
            throw new ArgumentException("StartedAt is required", nameof(startedAt));

        if (Status is EmailCampaignStatus.Sending or EmailCampaignStatus.Sent or EmailCampaignStatus.PartiallyFailed or EmailCampaignStatus.Failed or EmailCampaignStatus.Cancelled)
            throw new InvalidOperationException("Campaign cannot be started in its current status");

        Status = EmailCampaignStatus.Sending;
        StartedAt = startedAt;
    }

    public void Complete(DateTime completedAt)
    {
        if (completedAt == default)
            throw new ArgumentException("CompletedAt is required", nameof(completedAt));

        RecalculateCounts();
        CompletedAt = completedAt;

        if (SentCount == TotalRecipients && TotalRecipients > 0)
        {
            Status = EmailCampaignStatus.Sent;
            return;
        }

        if (SentCount > 0)
        {
            Status = EmailCampaignStatus.PartiallyFailed;
            return;
        }

        Status = EmailCampaignStatus.Failed;
    }

    public void MarkFailed(DateTime completedAt)
    {
        if (completedAt == default)
            throw new ArgumentException("CompletedAt is required", nameof(completedAt));

        RecalculateCounts();
        CompletedAt = completedAt;
        Status = EmailCampaignStatus.Failed;
    }

    public void RecalculateCounts()
    {
        TotalRecipients = _recipients.Count;
        SentCount = _recipients.Count(x => x.Status == EmailRecipientStatus.Sent);
        FailedCount = _recipients.Count(x => x.Status == EmailRecipientStatus.Failed);
        SkippedCount = _recipients.Count(x =>
            x.Status is EmailRecipientStatus.SkippedNoEmail or
                EmailRecipientStatus.SkippedRecentlySent or
                EmailRecipientStatus.SkippedMarketingDisabled);
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
}
