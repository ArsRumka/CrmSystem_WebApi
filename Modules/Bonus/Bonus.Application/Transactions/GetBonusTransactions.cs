using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using Bonus.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using FluentValidation;
using MediatR;

namespace Bonus.Application.Transactions;

public sealed record GetBonusTransactionsQuery(
    Guid? BonusAccountId,
    Guid? ClientId,
    Guid? DealId,
    BonusTransactionType? Type,
    DateTime? DateFrom,
    DateTime? DateTo) : IRequest<IReadOnlyList<BonusTransactionResponse>>;

public sealed class GetBonusTransactionsQueryValidator : AbstractValidator<GetBonusTransactionsQuery>
{
    public GetBonusTransactionsQueryValidator()
    {
        RuleFor(x => x.BonusAccountId).NotEqual(Guid.Empty).When(x => x.BonusAccountId.HasValue);
        RuleFor(x => x.ClientId).NotEqual(Guid.Empty).When(x => x.ClientId.HasValue);
        RuleFor(x => x.DealId).NotEqual(Guid.Empty).When(x => x.DealId.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom.Value <= x.DateTo.Value)
            .WithMessage("DateFrom must be less than or equal to DateTo");
    }
}

public sealed class GetBonusTransactionsQueryHandler
    : IRequestHandler<GetBonusTransactionsQuery, IReadOnlyList<BonusTransactionResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusTransactionRepository _transactionRepository;

    public GetBonusTransactionsQueryHandler(
        ICurrentUserService currentUserService,
        IBonusTransactionRepository transactionRepository)
    {
        _currentUserService = currentUserService;
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyList<BonusTransactionResponse>> Handle(
        GetBonusTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);

        var transactions = await _transactionRepository.SearchAsync(
            organizationId,
            request.BonusAccountId,
            request.ClientId,
            request.DealId,
            request.Type,
            request.DateFrom,
            request.DateTo,
            cancellationToken);

        return transactions.Select(transaction => transaction.ToResponse()).ToList();
    }
}
