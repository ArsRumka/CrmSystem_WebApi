using Email.Domain.Entities;

namespace Email.Application.Common;

public static class EmailAuditSnapshots
{
    public static object Settings(EmailSettings settings)
    {
        return new
        {
            settings.SenderName,
            settings.SenderEmail,
            settings.SmtpHost,
            settings.SmtpPort,
            settings.UseSsl,
            settings.Username,
            settings.IsEnabled
        };
    }

    public static object Template(EmailTemplate template)
    {
        return new
        {
            template.Name,
            template.Subject,
            template.IsHtml,
            template.IsActive
        };
    }

    public static object Campaign(EmailCampaign campaign)
    {
        return new
        {
            campaign.Name,
            campaign.TemplateId,
            campaign.Type,
            campaign.Status,
            campaign.TotalRecipients,
            campaign.SentCount,
            campaign.FailedCount,
            campaign.SkippedCount
        };
    }

    public static object AutomationRule(EmailAutomationRule rule)
    {
        return new
        {
            rule.IsEnabled,
            rule.TemplateId,
            rule.InactivityDays,
            rule.RepeatAfterDays,
            rule.LastRunAt
        };
    }

    public static object AutomationRun(
        EmailAutomationRule rule,
        bool campaignCreated,
        Guid? campaignId,
        int candidateCount,
        int totalRecipients,
        int sentCount,
        int failedCount,
        int skippedCount)
    {
        return new
        {
            rule.IsEnabled,
            rule.TemplateId,
            rule.InactivityDays,
            rule.RepeatAfterDays,
            rule.LastRunAt,
            CampaignCreated = campaignCreated,
            CampaignId = campaignId,
            CandidateCount = candidateCount,
            TotalRecipients = totalRecipients,
            SentCount = sentCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount
        };
    }
}
