using Email.Domain.Entities;
using Email.Domain.Enums;

namespace Email.Application.Abstractions.Repositories;

public interface IEmailCampaignRecipientRepository
{
    Task AddRangeAsync(IEnumerable<EmailCampaignRecipient> recipients, CancellationToken cancellationToken);

    Task<List<EmailCampaignRecipient>> GetByCampaignIdAsync(
        Guid organizationId,
        Guid campaignId,
        CancellationToken cancellationToken);

    Task<DateTime?> GetLastSentAtForClientAsync(
        Guid organizationId,
        Guid clientId,
        EmailCampaignType type,
        CancellationToken cancellationToken);
}
