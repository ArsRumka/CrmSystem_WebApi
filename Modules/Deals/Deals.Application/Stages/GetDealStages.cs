using BuildingBlocks.Application.Abstractions.Auth;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Abstractions.Services;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Stages;

public sealed record GetDealStagesQuery(string? Search, bool? IsActive) : IRequest<IReadOnlyList<DealStageResponse>>;

public sealed class GetDealStagesQueryValidator : AbstractValidator<GetDealStagesQuery>
{
    public GetDealStagesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class GetDealStagesQueryHandler : IRequestHandler<GetDealStagesQuery, IReadOnlyList<DealStageResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealStageInitializer _dealStageInitializer;
    private readonly IDealStageRepository _dealStageRepository;

    public GetDealStagesQueryHandler(
        ICurrentUserService currentUserService,
        IDealStageInitializer dealStageInitializer,
        IDealStageRepository dealStageRepository)
    {
        _currentUserService = currentUserService;
        _dealStageInitializer = dealStageInitializer;
        _dealStageRepository = dealStageRepository;
    }

    public async Task<IReadOnlyList<DealStageResponse>> Handle(
        GetDealStagesQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        await _dealStageInitializer.EnsureDefaultStagesAsync(organizationId, cancellationToken);

        var stages = await _dealStageRepository.SearchAsync(
            organizationId,
            request.Search,
            request.IsActive,
            cancellationToken);

        return stages.Select(stage => stage.ToResponse()).ToList();
    }
}
