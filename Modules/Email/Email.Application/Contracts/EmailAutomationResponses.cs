using Email.Domain.Entities;

namespace Email.Application.Contracts;

public sealed record EmailAutomationRuleResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? TemplateId,
    bool IsEnabled,
    int InactivityDays,
    int RepeatAfterDays,
    DateTime? LastRunAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? UpdatedByUserId);

public sealed record EmailAutomationRunResponse(
    bool IsEnabled,
    bool CampaignCreated,
    Guid? CampaignId,
    int CandidateCount,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    int SkippedCount,
    string Message);

internal static class EmailAutomationResponseMapper
{
    public static EmailAutomationRuleResponse ToResponse(this EmailAutomationRule rule)
    {
        return new EmailAutomationRuleResponse(
            rule.Id,
            rule.OrganizationId,
            rule.TemplateId,
            rule.IsEnabled,
            rule.InactivityDays,
            rule.RepeatAfterDays,
            rule.LastRunAt,
            rule.CreatedAt,
            rule.UpdatedAt,
            rule.UpdatedByUserId);
    }
}
