using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Abstractions.Services;
using Email.Application.Common;
using Email.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Email.Application.Campaigns;

public sealed record SendEmailCampaignCommand(Guid Id) : IRequest<EmailCampaignResponse>;

public sealed class SendEmailCampaignCommandValidator : AbstractValidator<SendEmailCampaignCommand>
{
    public SendEmailCampaignCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class SendEmailCampaignCommandHandler
    : IRequestHandler<SendEmailCampaignCommand, EmailCampaignResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailCampaignSender _campaignSender;
    private readonly IEmailCampaignRepository _campaignRepository;

    public SendEmailCampaignCommandHandler(
        ICurrentUserService currentUserService,
        IEmailCampaignSender campaignSender,
        IEmailCampaignRepository campaignRepository)
    {
        _currentUserService = currentUserService;
        _campaignSender = campaignSender;
        _campaignRepository = campaignRepository;
    }

    public async Task<EmailCampaignResponse> Handle(
        SendEmailCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        await _campaignSender.SendCampaignAsync(organizationId, request.Id, cancellationToken);

        var campaign = await _campaignRepository.GetByIdWithRecipientsAsync(
            organizationId,
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Email campaign was not found");

        return campaign.ToResponse();
    }
}
