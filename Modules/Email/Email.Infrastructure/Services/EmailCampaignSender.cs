using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Abstractions.Services;
using Email.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Email.Infrastructure.Services;

public sealed class EmailCampaignSender : IEmailCampaignSender
{
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IEmailSettingsRepository _settingsRepository;
    private readonly IEmailClientLookupService _clientLookupService;
    private readonly IEmailOrganizationLookupService _organizationLookupService;
    private readonly IEmailPasswordProtector _passwordProtector;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IOrganizationSmtpEmailSender _smtpEmailSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailCampaignSender> _logger;

    public EmailCampaignSender(
        IEmailCampaignRepository campaignRepository,
        IEmailTemplateRepository templateRepository,
        IEmailSettingsRepository settingsRepository,
        IEmailClientLookupService clientLookupService,
        IEmailOrganizationLookupService organizationLookupService,
        IEmailPasswordProtector passwordProtector,
        IEmailTemplateRenderer templateRenderer,
        IOrganizationSmtpEmailSender smtpEmailSender,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<EmailCampaignSender> logger)
    {
        _campaignRepository = campaignRepository;
        _templateRepository = templateRepository;
        _settingsRepository = settingsRepository;
        _clientLookupService = clientLookupService;
        _organizationLookupService = organizationLookupService;
        _passwordProtector = passwordProtector;
        _templateRenderer = templateRenderer;
        _smtpEmailSender = smtpEmailSender;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SendCampaignAsync(
        Guid organizationId,
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdWithRecipientsAsync(
            organizationId,
            campaignId,
            cancellationToken)
            ?? throw new NotFoundException("Email campaign was not found");

        if (campaign.Status != EmailCampaignStatus.Draft)
        {
            throw new ConflictException("Only draft campaigns can be sent");
        }

        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken)
            ?? throw new ConflictException("Email settings are not configured");

        if (!settings.IsEnabled)
        {
            throw new ConflictException("Email settings are disabled");
        }

        var template = await _templateRepository.GetByIdAsync(organizationId, campaign.TemplateId, cancellationToken)
            ?? throw new NotFoundException("Email template was not found");

        if (!template.IsActive)
        {
            throw new ConflictException("Email template is inactive");
        }

        string smtpPassword;
        try
        {
            smtpPassword = _passwordProtector.Unprotect(settings.PasswordEncrypted);
        }
        catch (Exception ex)
        {
            throw new ConflictException($"SMTP password could not be decrypted: {ex.Message}");
        }

        var pendingRecipients = campaign.Recipients
            .Where(x => x.Status == EmailRecipientStatus.Pending)
            .ToList();

        var clients = await _clientLookupService.GetClientsByIdsAsync(
            organizationId,
            pendingRecipients.Select(x => x.ClientId).Distinct().ToList(),
            cancellationToken);

        var clientsById = clients.ToDictionary(x => x.ClientId);
        var organizationName = await _organizationLookupService.GetOrganizationNameAsync(organizationId, cancellationToken);

        campaign.Start(_dateTimeProvider.UtcNow);

        foreach (var recipient in pendingRecipients)
        {
            if (!clientsById.TryGetValue(recipient.ClientId, out var client) || !client.IsActive)
            {
                recipient.MarkFailed("Client was not found");
                continue;
            }

            if (!client.AllowMarketingEmails)
            {
                recipient.MarkSkippedMarketingDisabled();
                continue;
            }

            if (string.IsNullOrWhiteSpace(recipient.Email))
            {
                recipient.MarkSkippedNoEmail();
                continue;
            }

            var rendered = _templateRenderer.Render(new EmailTemplateRenderRequest(
                template.Subject,
                template.Body,
                client.FirstName,
                client.LastName,
                client.MiddleName,
                client.FullName,
                organizationName,
                recipient.LastDealDate,
                recipient.DaysSinceLastDeal));

            try
            {
                await _smtpEmailSender.SendAsync(
                    new OrganizationEmailMessage(
                        settings.SenderName,
                        settings.SenderEmail,
                        recipient.Email,
                        rendered.Subject,
                        rendered.Body,
                        template.IsHtml,
                        settings.SmtpHost,
                        settings.SmtpPort,
                        settings.UseSsl,
                        settings.Username,
                        smtpPassword),
                    cancellationToken);

                recipient.MarkSent(_dateTimeProvider.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Email campaign recipient failed. CampaignId: {CampaignId}; RecipientId: {RecipientId}",
                    campaign.Id,
                    recipient.Id);

                recipient.MarkFailed(ex.Message);
            }
        }

        campaign.Complete(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
