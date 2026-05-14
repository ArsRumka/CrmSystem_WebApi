using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Email.Application.Campaigns;

public sealed record GetEmailCampaignRecipientsQuery(Guid CampaignId)
    : IRequest<IReadOnlyList<EmailCampaignRecipientResponse>>;

public sealed class GetEmailCampaignRecipientsQueryValidator
    : AbstractValidator<GetEmailCampaignRecipientsQuery>
{
    public GetEmailCampaignRecipientsQueryValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}

public sealed class GetEmailCampaignRecipientsQueryHandler
    : IRequestHandler<GetEmailCampaignRecipientsQuery, IReadOnlyList<EmailCampaignRecipientResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailCampaignRepository _campaignRepository;
    private readonly IEmailCampaignRecipientRepository _recipientRepository;

    public GetEmailCampaignRecipientsQueryHandler(
        ICurrentUserService currentUserService,
        IEmailCampaignRepository campaignRepository,
        IEmailCampaignRecipientRepository recipientRepository)
    {
        _currentUserService = currentUserService;
        _campaignRepository = campaignRepository;
        _recipientRepository = recipientRepository;
    }

    public async Task<IReadOnlyList<EmailCampaignRecipientResponse>> Handle(
        GetEmailCampaignRecipientsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var campaign = await _campaignRepository.GetByIdAsync(organizationId, request.CampaignId, cancellationToken)
            ?? throw new NotFoundException("Email campaign was not found");

        var recipients = await _recipientRepository.GetByCampaignIdAsync(
            organizationId,
            campaign.Id,
            cancellationToken);

        return recipients.Select(x => x.ToResponse()).ToList();
    }
}
