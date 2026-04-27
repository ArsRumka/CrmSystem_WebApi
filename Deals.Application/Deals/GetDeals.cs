using BuildingBlocks.Application.Abstractions.Auth;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Deals;

public sealed record GetDealsQuery(
    string? Search,
    Guid? ClientId,
    Guid? ResponsibleUserId,
    Guid? StageId,
    DateTime? DateFrom,
    DateTime? DateTo,
    bool? IsActive) : IRequest<IReadOnlyList<DealResponse>>;

public sealed class GetDealsQueryValidator : AbstractValidator<GetDealsQuery>
{
    public GetDealsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.ClientId).NotEqual(Guid.Empty).When(x => x.ClientId.HasValue);
        RuleFor(x => x.ResponsibleUserId).NotEqual(Guid.Empty).When(x => x.ResponsibleUserId.HasValue);
        RuleFor(x => x.StageId).NotEqual(Guid.Empty).When(x => x.StageId.HasValue);
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom.Value <= x.DateTo.Value)
            .WithMessage("DateFrom must be less than or equal to DateTo");
    }
}

public sealed class GetDealsQueryHandler : IRequestHandler<GetDealsQuery, IReadOnlyList<DealResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;

    public GetDealsQueryHandler(
        ICurrentUserService currentUserService,
        IDealRepository dealRepository,
        IDealStageRepository dealStageRepository)
    {
        _currentUserService = currentUserService;
        _dealRepository = dealRepository;
        _dealStageRepository = dealStageRepository;
    }

    public async Task<IReadOnlyList<DealResponse>> Handle(
        GetDealsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var deals = await _dealRepository.SearchAsync(
            organizationId,
            request.Search,
            request.ClientId,
            request.ResponsibleUserId,
            request.StageId,
            request.DateFrom,
            request.DateTo,
            request.IsActive,
            cancellationToken);

        var stages = await _dealStageRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        var stageNames = stages.ToDictionary(stage => stage.Id, stage => stage.Name);

        return deals
            .Select(deal => deal.ToResponse(stageNames.GetValueOrDefault(deal.StageId)))
            .ToList();
    }
}
