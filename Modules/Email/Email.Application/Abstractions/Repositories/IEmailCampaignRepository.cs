using Email.Domain.Entities;
using Email.Domain.Enums;

namespace Email.Application.Abstractions.Repositories;

public interface IEmailCampaignRepository
{
    Task AddAsync(EmailCampaign campaign, CancellationToken cancellationToken);

    Task<EmailCampaign?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<EmailCampaign?> GetByIdWithRecipientsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<EmailCampaign>> SearchAsync(
        Guid organizationId,
        EmailCampaignType? type,
        EmailCampaignStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken);
}
