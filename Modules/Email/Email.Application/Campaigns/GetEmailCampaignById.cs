using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Email.Application.Campaigns;

public sealed record GetEmailCampaignByIdQuery(Guid Id) : IRequest<EmailCampaignResponse>;

public sealed class GetEmailCampaignByIdQueryValidator : AbstractValidator<GetEmailCampaignByIdQuery>
{
    public GetEmailCampaignByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetEmailCampaignByIdQueryHandler
    : IRequestHandler<GetEmailCampaignByIdQuery, EmailCampaignResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailCampaignRepository _campaignRepository;

    public GetEmailCampaignByIdQueryHandler(
        ICurrentUserService currentUserService,
        IEmailCampaignRepository campaignRepository)
    {
        _currentUserService = currentUserService;
        _campaignRepository = campaignRepository;
    }

    public async Task<EmailCampaignResponse> Handle(
        GetEmailCampaignByIdQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var campaign = await _campaignRepository.GetByIdWithRecipientsAsync(
            organizationId,
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Email campaign was not found");

        return campaign.ToResponse();
    }
}
