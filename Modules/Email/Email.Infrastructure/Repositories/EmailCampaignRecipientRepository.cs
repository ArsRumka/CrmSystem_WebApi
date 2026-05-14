using Email.Application.Abstractions.Repositories;
using Email.Domain.Entities;
using Email.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Email.Infrastructure.Repositories;

public sealed class EmailCampaignRecipientRepository : IEmailCampaignRecipientRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailCampaignRecipientRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(IEnumerable<EmailCampaignRecipient> recipients, CancellationToken cancellationToken)
    {
        await _dbContext.Set<EmailCampaignRecipient>().AddRangeAsync(recipients, cancellationToken);
    }

    public async Task<List<EmailCampaignRecipient>> GetByCampaignIdAsync(
        Guid organizationId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<EmailCampaignRecipient>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.CampaignId == campaignId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<DateTime?> GetLastSentAtForClientAsync(
        Guid organizationId,
        Guid clientId,
        EmailCampaignType type,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmailCampaignRecipient>()
            .AsNoTracking()
            .Join(
                _dbContext.Set<EmailCampaign>().AsNoTracking(),
                recipient => recipient.CampaignId,
                campaign => campaign.Id,
                (recipient, campaign) => new { recipient, campaign })
            .Where(x =>
                x.recipient.OrganizationId == organizationId &&
                x.recipient.ClientId == clientId &&
                x.recipient.Status == EmailRecipientStatus.Sent &&
                x.campaign.OrganizationId == organizationId &&
                x.campaign.Type == type)
            .MaxAsync(x => x.recipient.SentAt, cancellationToken);
    }
}
