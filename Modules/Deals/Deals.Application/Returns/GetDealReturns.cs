using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Returns;

public sealed record GetDealReturnsQuery(Guid DealId) : IRequest<IReadOnlyList<DealReturnResponse>>;

public sealed class GetDealReturnsQueryValidator : AbstractValidator<GetDealReturnsQuery>
{
    public GetDealReturnsQueryValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
    }
}

public sealed class GetDealReturnsQueryHandler : IRequestHandler<GetDealReturnsQuery, IReadOnlyList<DealReturnResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealReturnRepository _dealReturnRepository;

    public GetDealReturnsQueryHandler(
        ICurrentUserService currentUserService,
        IDealRepository dealRepository,
        IDealReturnRepository dealReturnRepository)
    {
        _currentUserService = currentUserService;
        _dealRepository = dealRepository;
        _dealReturnRepository = dealReturnRepository;
    }

    public async Task<IReadOnlyList<DealReturnResponse>> Handle(
        GetDealReturnsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        if (await _dealRepository.GetByIdAsync(organizationId, request.DealId, cancellationToken) is null)
        {
            throw new NotFoundException("Deal was not found");
        }

        var returns = await _dealReturnRepository.GetByDealIdAsync(
            organizationId,
            request.DealId,
            cancellationToken);

        return returns.Select(x => x.ToResponse()).ToList();
    }
}
