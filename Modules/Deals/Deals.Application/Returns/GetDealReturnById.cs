using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Returns;

public sealed record GetDealReturnByIdQuery(Guid Id) : IRequest<DealReturnResponse>;

public sealed class GetDealReturnByIdQueryValidator : AbstractValidator<GetDealReturnByIdQuery>
{
    public GetDealReturnByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetDealReturnByIdQueryHandler : IRequestHandler<GetDealReturnByIdQuery, DealReturnResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealReturnRepository _dealReturnRepository;

    public GetDealReturnByIdQueryHandler(
        ICurrentUserService currentUserService,
        IDealReturnRepository dealReturnRepository)
    {
        _currentUserService = currentUserService;
        _dealReturnRepository = dealReturnRepository;
    }

    public async Task<DealReturnResponse> Handle(
        GetDealReturnByIdQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var dealReturn = await _dealReturnRepository.GetByIdWithItemsAsync(
            organizationId,
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Deal return was not found");

        return dealReturn.ToResponse();
    }
}
