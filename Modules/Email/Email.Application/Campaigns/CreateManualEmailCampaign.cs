using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Abstractions.Services;
using Email.Application.Common;
using Email.Application.Contracts;
using Email.Domain.Entities;
using Email.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Email.Application.Campaigns;

public sealed record CreateManualEmailCampaignCommand(
    string Name,
    Guid TemplateId,
    IReadOnlyList<Guid> ClientIds) : IRequest<EmailCampaignResponse>;

public sealed class CreateManualEmailCampaignCommandValidator
    : AbstractValidator<CreateManualEmailCampaignCommand>
{
    public CreateManualEmailCampaignCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.ClientIds).NotNull().NotEmpty();
        RuleForEach(x => x.ClientIds).NotEmpty();
        RuleFor(x => x.ClientIds)
            .Must(HaveDistinctClientIds)
            .WithMessage("ClientIds must be distinct");
    }

    private static bool HaveDistinctClientIds(IReadOnlyList<Guid>? clientIds)
    {
        return clientIds is null || clientIds.Distinct().Count() == clientIds.Count;
    }
}

public sealed class CreateManualEmailCampaignCommandHandler
    : IRequestHandler<CreateManualEmailCampaignCommand, EmailCampaignResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly IEmailClientLookupService _clientLookupService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateManualEmailCampaignCommandHandler(
        ICurrentUserService currentUserService,
        IEmailTemplateRepository templateRepository,
        IEmailCampaignRepository campaignRepository,
        IEmailClientLookupService clientLookupService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _templateRepository = templateRepository;
        _campaignRepository = campaignRepository;
        _clientLookupService = clientLookupService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailCampaignResponse> Handle(
        CreateManualEmailCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var template = await _templateRepository.GetByIdAsync(organizationId, request.TemplateId, cancellationToken)
            ?? throw new NotFoundException("Email template was not found");

        if (!template.IsActive)
        {
            throw new ConflictException("Email template is inactive");
        }

        var clients = await _clientLookupService.GetClientsByIdsAsync(
            organizationId,
            request.ClientIds,
            cancellationToken);

        if (clients.Count != request.ClientIds.Count || clients.Any(x => !x.IsActive))
        {
            throw new NotFoundException("One or more clients were not found");
        }

        var now = _dateTimeProvider.UtcNow;
        var campaign = new EmailCampaign(
            Guid.NewGuid(),
            organizationId,
            template.Id,
            request.Name,
            EmailCampaignType.Manual,
            userId,
            now);

        var recipients = clients.Select(client => CreateRecipient(
            organizationId,
            campaign.Id,
            client.ClientId,
            client.Email,
            client.FullName,
            lastDealDate: null,
            daysSinceLastDeal: null,
            client.AllowMarketingEmails,
            now));

        campaign.AddRecipients(recipients);

        await _campaignRepository.AddAsync(campaign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return campaign.ToResponse();
    }

    private static EmailCampaignRecipient CreateRecipient(
        Guid organizationId,
        Guid campaignId,
        Guid clientId,
        string? email,
        string fullName,
        DateTime? lastDealDate,
        int? daysSinceLastDeal,
        bool allowMarketingEmails,
        DateTime createdAt)
    {
        var status = EmailRecipientStatus.Pending;

        if (!allowMarketingEmails)
        {
            status = EmailRecipientStatus.SkippedMarketingDisabled;
        }
        else if (string.IsNullOrWhiteSpace(email))
        {
            status = EmailRecipientStatus.SkippedNoEmail;
        }

        return new EmailCampaignRecipient(
            Guid.NewGuid(),
            organizationId,
            campaignId,
            clientId,
            email,
            fullName,
            lastDealDate,
            daysSinceLastDeal,
            status,
            createdAt);
    }
}
