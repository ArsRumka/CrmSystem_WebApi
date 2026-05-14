using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Abstractions.Services;
using Email.Application.Contracts;
using Email.Domain.Entities;
using Email.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Email.Infrastructure.Services;

public sealed class EmailAutomationRunner : IEmailAutomationRunner
{
    private readonly IEmailAutomationRuleRepository _ruleRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IEmailSettingsRepository _settingsRepository;
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly IEmailClientLookupService _clientLookupService;
    private readonly IEmailCampaignSender _campaignSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailAutomationRunner> _logger;

    public EmailAutomationRunner(
        IEmailAutomationRuleRepository ruleRepository,
        IEmailTemplateRepository templateRepository,
        IEmailSettingsRepository settingsRepository,
        IEmailCampaignRepository campaignRepository,
        IEmailClientLookupService clientLookupService,
        IEmailCampaignSender campaignSender,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<EmailAutomationRunner> logger)
    {
        _ruleRepository = ruleRepository;
        _templateRepository = templateRepository;
        _settingsRepository = settingsRepository;
        _campaignRepository = campaignRepository;
        _clientLookupService = clientLookupService;
        _campaignSender = campaignSender;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EmailAutomationRunResponse> RunForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var rule = await _ruleRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        if (rule is null)
        {
            rule = EmailAutomationRule.CreateDefault(organizationId, now);
            rule.MarkRun(now);
            await _ruleRepository.AddAsync(rule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new EmailAutomationRunResponse(
                IsEnabled: false,
                CampaignCreated: false,
                CampaignId: null,
                CandidateCount: 0,
                TotalRecipients: 0,
                SentCount: 0,
                FailedCount: 0,
                SkippedCount: 0,
                "Automation rule is disabled");
        }

        rule.MarkRun(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!rule.IsEnabled)
        {
            return new EmailAutomationRunResponse(
                IsEnabled: false,
                CampaignCreated: false,
                CampaignId: null,
                CandidateCount: 0,
                TotalRecipients: 0,
                SentCount: 0,
                FailedCount: 0,
                SkippedCount: 0,
                "Automation rule is disabled");
        }

        if (rule.TemplateId is null)
        {
            throw new ConflictException("Automation template is not configured");
        }

        var template = await _templateRepository.GetByIdAsync(organizationId, rule.TemplateId.Value, cancellationToken)
            ?? throw new NotFoundException("Email template was not found");

        if (!template.IsActive)
        {
            throw new ConflictException("Email template is inactive");
        }

        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken)
            ?? throw new ConflictException("Email settings are not configured");

        if (!settings.IsEnabled)
        {
            throw new ConflictException("Email settings are disabled");
        }

        var candidates = await _clientLookupService.GetInactiveClientsAsync(
            organizationId,
            rule.InactivityDays,
            rule.RepeatAfterDays,
            cancellationToken);

        if (candidates.Count == 0)
        {
            return new EmailAutomationRunResponse(
                IsEnabled: true,
                CampaignCreated: false,
                CampaignId: null,
                CandidateCount: 0,
                TotalRecipients: 0,
                SentCount: 0,
                FailedCount: 0,
                SkippedCount: 0,
                "No inactive clients were found");
        }

        var campaign = new EmailCampaign(
            Guid.NewGuid(),
            organizationId,
            template.Id,
            $"Inactive clients automation {now:yyyy-MM-dd HH:mm}",
            EmailCampaignType.AutomaticInactiveClients,
            createdByUserId: null,
            now);

        var recipients = candidates.Select(candidate => CreateRecipient(
            organizationId,
            campaign.Id,
            candidate,
            now));

        campaign.AddRecipients(recipients);

        await _campaignRepository.AddAsync(campaign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _campaignSender.SendCampaignAsync(organizationId, campaign.Id, cancellationToken);

        var completedCampaign = await _campaignRepository.GetByIdWithRecipientsAsync(
            organizationId,
            campaign.Id,
            cancellationToken);

        return new EmailAutomationRunResponse(
            IsEnabled: true,
            CampaignCreated: true,
            CampaignId: campaign.Id,
            CandidateCount: candidates.Count,
            TotalRecipients: completedCampaign?.TotalRecipients ?? campaign.TotalRecipients,
            SentCount: completedCampaign?.SentCount ?? campaign.SentCount,
            FailedCount: completedCampaign?.FailedCount ?? campaign.FailedCount,
            SkippedCount: completedCampaign?.SkippedCount ?? campaign.SkippedCount,
            "Automation campaign was created");
    }

    public async Task RunAllAsync(CancellationToken cancellationToken)
    {
        var rules = await _ruleRepository.GetEnabledRulesAsync(cancellationToken);

        foreach (var rule in rules)
        {
            try
            {
                await RunForOrganizationAsync(rule.OrganizationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Email automation failed for organization {OrganizationId}",
                    rule.OrganizationId);
            }
        }
    }

    private static EmailCampaignRecipient CreateRecipient(
        Guid organizationId,
        Guid campaignId,
        InactiveClientEmailCandidate candidate,
        DateTime createdAt)
    {
        var status = EmailRecipientStatus.Pending;

        if (!candidate.AllowMarketingEmails)
        {
            status = EmailRecipientStatus.SkippedMarketingDisabled;
        }
        else if (string.IsNullOrWhiteSpace(candidate.Email))
        {
            status = EmailRecipientStatus.SkippedNoEmail;
        }

        return new EmailCampaignRecipient(
            Guid.NewGuid(),
            organizationId,
            campaignId,
            candidate.ClientId,
            candidate.Email,
            candidate.FullName,
            candidate.LastDealDate,
            candidate.DaysSinceLastDeal,
            status,
            createdAt);
    }
}
