using BuildingBlocks.Application.Abstractions.Auth;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using Email.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Email.Application.Campaigns;

public sealed record GetEmailCampaignsQuery(
    EmailCampaignType? Type,
    EmailCampaignStatus? Status,
    DateTime? DateFrom,
    DateTime? DateTo) : IRequest<IReadOnlyList<EmailCampaignResponse>>;

public sealed class GetEmailCampaignsQueryValidator : AbstractValidator<GetEmailCampaignsQuery>
{
    public GetEmailCampaignsQueryValidator()
    {
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("dateFrom must be less than or equal to dateTo");
    }
}

public sealed class GetEmailCampaignsQueryHandler
    : IRequestHandler<GetEmailCampaignsQuery, IReadOnlyList<EmailCampaignResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailCampaignRepository _campaignRepository;

    public GetEmailCampaignsQueryHandler(
        ICurrentUserService currentUserService,
        IEmailCampaignRepository campaignRepository)
    {
        _currentUserService = currentUserService;
        _campaignRepository = campaignRepository;
    }

    public async Task<IReadOnlyList<EmailCampaignResponse>> Handle(
        GetEmailCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var campaigns = await _campaignRepository.SearchAsync(
            organizationId,
            request.Type,
            request.Status,
            request.DateFrom,
            request.DateTo,
            cancellationToken);

        return campaigns.Select(x => x.ToResponse(includeRecipients: false)).ToList();
    }
}
