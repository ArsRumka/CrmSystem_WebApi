using BuildingBlocks.Application.Abstractions.Time;
using Clients.Domain.Entities;
using Deals.Domain.Entities;
using Email.Application.Abstractions.Services;
using Email.Domain.Entities;
using Email.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Email.Infrastructure.Services;

public sealed class EmailClientLookupService : IEmailClientLookupService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EmailClientLookupService(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<List<EmailClientInfo>> GetClientsByIdsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Client>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && clientIds.Contains(x.Id))
            .Select(x => new EmailClientInfo(
                x.Id,
                x.FirstName,
                x.LastName,
                x.MiddleName,
                x.MiddleName == null ? x.LastName + " " + x.FirstName : x.LastName + " " + x.FirstName + " " + x.MiddleName,
                x.Email,
                x.IsActive,
                x.AllowMarketingEmails))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<InactiveClientEmailCandidate>> GetInactiveClientsAsync(
        Guid organizationId,
        int inactivityDays,
        int repeatAfterDays,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var inactivityCutoff = now.AddDays(-inactivityDays);
        var repeatCutoff = now.AddDays(-repeatAfterDays);

        var successfulFinalDeals =
            from deal in _dbContext.Set<Deal>().AsNoTracking()
            join stage in _dbContext.Set<DealStage>().AsNoTracking()
                on deal.StageId equals stage.Id
            where deal.OrganizationId == organizationId &&
                  stage.OrganizationId == organizationId &&
                  deal.IsActive &&
                  stage.IsFinal &&
                  stage.IsSuccessful &&
                  deal.ClosedAt != null
            group deal by deal.ClientId
            into grouped
            select new
            {
                ClientId = grouped.Key,
                LastDealDate = grouped.Max(x => x.ClosedAt!.Value)
            };

        var recentlySentClientIds =
            from recipient in _dbContext.Set<EmailCampaignRecipient>().AsNoTracking()
            join campaign in _dbContext.Set<EmailCampaign>().AsNoTracking()
                on recipient.CampaignId equals campaign.Id
            where recipient.OrganizationId == organizationId &&
                  campaign.OrganizationId == organizationId &&
                  campaign.Type == EmailCampaignType.AutomaticInactiveClients &&
                  recipient.Status == EmailRecipientStatus.Sent &&
                  recipient.SentAt != null &&
                  recipient.SentAt >= repeatCutoff
            select recipient.ClientId;

        var rows = await (
            from client in _dbContext.Set<Client>().AsNoTracking()
            join deal in successfulFinalDeals
                on client.Id equals deal.ClientId
            where client.OrganizationId == organizationId &&
                  client.IsActive &&
                  deal.LastDealDate <= inactivityCutoff &&
                  !recentlySentClientIds.Contains(client.Id)
            orderby deal.LastDealDate
            select new
            {
                client.Id,
                client.FirstName,
                client.LastName,
                client.MiddleName,
                client.Email,
                client.AllowMarketingEmails,
                deal.LastDealDate
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new InactiveClientEmailCandidate(
                x.Id,
                x.FirstName,
                x.LastName,
                x.MiddleName,
                x.MiddleName == null ? x.LastName + " " + x.FirstName : x.LastName + " " + x.FirstName + " " + x.MiddleName,
                x.Email,
                x.AllowMarketingEmails,
                x.LastDealDate,
                Math.Max(0, (int)(now.Date - x.LastDealDate.Date).TotalDays)))
            .ToList();
    }
}
