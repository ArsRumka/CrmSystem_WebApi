using Email.Application.Abstractions.Repositories;
using Email.Domain.Entities;
using Email.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Email.Infrastructure.Repositories;

public sealed class EmailCampaignRepository : IEmailCampaignRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailCampaignRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailCampaign campaign, CancellationToken cancellationToken)
    {
        await _dbContext.Set<EmailCampaign>().AddAsync(campaign, cancellationToken);
    }

    public Task<EmailCampaign?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmailCampaign>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public Task<EmailCampaign?> GetByIdWithRecipientsAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmailCampaign>()
            .Include(x => x.Recipients)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<EmailCampaign>> SearchAsync(
        Guid organizationId,
        EmailCampaignType? type,
        EmailCampaignStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<EmailCampaign>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dateTo.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
