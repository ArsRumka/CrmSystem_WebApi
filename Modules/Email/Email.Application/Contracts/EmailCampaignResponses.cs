using Email.Domain.Entities;
using Email.Domain.Enums;

namespace Email.Application.Contracts;

public sealed record EmailCampaignRecipientResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CampaignId,
    Guid ClientId,
    string? Email,
    string? FullNameSnapshot,
    DateTime? LastDealDate,
    int? DaysSinceLastDeal,
    EmailRecipientStatus Status,
    string? ErrorMessage,
    DateTime? SentAt,
    DateTime CreatedAt);

public sealed record EmailCampaignResponse(
    Guid Id,
    Guid OrganizationId,
    Guid TemplateId,
    string Name,
    EmailCampaignType Type,
    EmailCampaignStatus Status,
    DateTime CreatedAt,
    Guid? CreatedByUserId,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<EmailCampaignRecipientResponse> Recipients);

internal static class EmailCampaignResponseMapper
{
    public static EmailCampaignResponse ToResponse(this EmailCampaign campaign, bool includeRecipients = true)
    {
        return new EmailCampaignResponse(
            campaign.Id,
            campaign.OrganizationId,
            campaign.TemplateId,
            campaign.Name,
            campaign.Type,
            campaign.Status,
            campaign.CreatedAt,
            campaign.CreatedByUserId,
            campaign.StartedAt,
            campaign.CompletedAt,
            campaign.TotalRecipients,
            campaign.SentCount,
            campaign.FailedCount,
            campaign.SkippedCount,
            includeRecipients
                ? campaign.Recipients.Select(x => x.ToResponse()).ToList()
                : []);
    }

    public static EmailCampaignRecipientResponse ToResponse(this EmailCampaignRecipient recipient)
    {
        return new EmailCampaignRecipientResponse(
            recipient.Id,
            recipient.OrganizationId,
            recipient.CampaignId,
            recipient.ClientId,
            recipient.Email,
            recipient.FullNameSnapshot,
            recipient.LastDealDate,
            recipient.DaysSinceLastDeal,
            recipient.Status,
            recipient.ErrorMessage,
            recipient.SentAt,
            recipient.CreatedAt);
    }
}
