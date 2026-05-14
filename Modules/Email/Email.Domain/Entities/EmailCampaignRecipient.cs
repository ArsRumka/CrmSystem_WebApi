using System.Net.Mail;
using Email.Domain.Enums;

namespace Email.Domain.Entities;

public class EmailCampaignRecipient
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid ClientId { get; private set; }
    public string? Email { get; private set; }
    public string? FullNameSnapshot { get; private set; }
    public DateTime? LastDealDate { get; private set; }
    public int? DaysSinceLastDeal { get; private set; }
    public EmailRecipientStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmailCampaignRecipient()
    {
    }

    public EmailCampaignRecipient(
        Guid id,
        Guid organizationId,
        Guid campaignId,
        Guid clientId,
        string? email,
        string? fullNameSnapshot,
        DateTime? lastDealDate,
        int? daysSinceLastDeal,
        EmailRecipientStatus status,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (campaignId == Guid.Empty)
            throw new ArgumentException("CampaignId is required", nameof(campaignId));

        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required", nameof(clientId));

        if (!Enum.IsDefined(status))
            throw new ArgumentException("Invalid recipient status", nameof(status));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        CampaignId = campaignId;
        ClientId = clientId;
        Email = NormalizeEmail(email, status);
        FullNameSnapshot = NormalizeOptional(fullNameSnapshot, nameof(fullNameSnapshot), 300);
        LastDealDate = lastDealDate;
        DaysSinceLastDeal = daysSinceLastDeal;
        Status = status;
        CreatedAt = createdAt;
    }

    public void MarkSent(DateTime sentAt)
    {
        if (sentAt == default)
            throw new ArgumentException("SentAt is required", nameof(sentAt));

        EnsureSendableEmail();
        Status = EmailRecipientStatus.Sent;
        ErrorMessage = null;
        SentAt = sentAt;
    }

    public void MarkFailed(string errorMessage)
    {
        EnsureSendableEmail();
        Status = EmailRecipientStatus.Failed;
        ErrorMessage = NormalizeOptional(errorMessage, nameof(errorMessage), 2000);
        SentAt = null;
    }

    public void MarkSkippedNoEmail()
    {
        Status = EmailRecipientStatus.SkippedNoEmail;
        ErrorMessage = null;
        SentAt = null;
    }

    public void MarkSkippedRecentlySent()
    {
        Status = EmailRecipientStatus.SkippedRecentlySent;
        ErrorMessage = null;
        SentAt = null;
    }

    public void MarkSkippedMarketingDisabled()
    {
        Status = EmailRecipientStatus.SkippedMarketingDisabled;
        ErrorMessage = null;
        SentAt = null;
    }

    private void EnsureSendableEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
            throw new InvalidOperationException("Recipient email is required");
    }

    private static string? NormalizeEmail(string? email, EmailRecipientStatus status)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            if (status == EmailRecipientStatus.Pending)
                throw new ArgumentException("Email is required for pending recipients", nameof(email));

            return null;
        }

        var normalized = email.Trim();
        if (normalized.Length > 320)
            throw new ArgumentException("Email cannot exceed 320 characters", nameof(email));

        try
        {
            _ = new MailAddress(normalized);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Email must be a valid email", nameof(email), ex);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }
}
