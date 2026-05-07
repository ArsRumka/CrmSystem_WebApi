using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;

namespace Bonus.Application.Transactions;

public sealed record GetBonusTransactionByIdQuery(Guid Id) : IRequest<BonusTransactionResponse>;

public sealed class GetBonusTransactionByIdQueryValidator : AbstractValidator<GetBonusTransactionByIdQuery>
{
    public GetBonusTransactionByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetBonusTransactionByIdQueryHandler
    : IRequestHandler<GetBonusTransactionByIdQuery, BonusTransactionResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusTransactionRepository _transactionRepository;

    public GetBonusTransactionByIdQueryHandler(
        ICurrentUserService currentUserService,
        IBonusTransactionRepository transactionRepository)
    {
        _currentUserService = currentUserService;
        _transactionRepository = transactionRepository;
    }

    public async Task<BonusTransactionResponse> Handle(
        GetBonusTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);

        var transaction = await _transactionRepository.GetByIdAsync(
                organizationId,
                request.Id,
                cancellationToken)
            ?? throw new NotFoundException("Bonus transaction was not found");

        return transaction.ToResponse();
    }
}
