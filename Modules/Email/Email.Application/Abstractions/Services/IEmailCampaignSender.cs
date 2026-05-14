namespace Email.Application.Abstractions.Services;

public interface IEmailCampaignSender
{
    Task SendCampaignAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken);
}
