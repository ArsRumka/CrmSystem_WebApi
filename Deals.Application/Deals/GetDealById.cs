using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Deals;

public sealed record GetDealByIdQuery(Guid Id) : IRequest<DealResponse>;

public sealed class GetDealByIdQueryValidator : AbstractValidator<GetDealByIdQuery>
{
    public GetDealByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetDealByIdQueryHandler : IRequestHandler<GetDealByIdQuery, DealResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;

    public GetDealByIdQueryHandler(
        ICurrentUserService currentUserService,
        IDealRepository dealRepository,
        IDealStageRepository dealStageRepository)
    {
        _currentUserService = currentUserService;
        _dealRepository = dealRepository;
        _dealStageRepository = dealStageRepository;
    }

    public async Task<DealResponse> Handle(GetDealByIdQuery request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var deal = await _dealRepository.GetByIdWithItemsAndHistoryAsync(
            organizationId,
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var stage = await _dealStageRepository.GetByIdAsync(organizationId, deal.StageId, cancellationToken);

        return deal.ToResponse(stage?.Name);
    }
}
