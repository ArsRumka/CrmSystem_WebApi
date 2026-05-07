using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using BuildingBlocks.Application.Abstractions.Auth;
using FluentValidation;
using MediatR;

namespace Bonus.Application.Accounts;

public sealed record GetBonusAccountsQuery(Guid? ClientId, bool? IsActive)
    : IRequest<IReadOnlyList<BonusAccountResponse>>;

public sealed class GetBonusAccountsQueryValidator : AbstractValidator<GetBonusAccountsQuery>
{
    public GetBonusAccountsQueryValidator()
    {
        RuleFor(x => x.ClientId).NotEqual(Guid.Empty).When(x => x.ClientId.HasValue);
    }
}

public sealed class GetBonusAccountsQueryHandler
    : IRequestHandler<GetBonusAccountsQuery, IReadOnlyList<BonusAccountResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusAccountRepository _accountRepository;

    public GetBonusAccountsQueryHandler(
        ICurrentUserService currentUserService,
        IBonusAccountRepository accountRepository)
    {
        _currentUserService = currentUserService;
        _accountRepository = accountRepository;
    }

    public async Task<IReadOnlyList<BonusAccountResponse>> Handle(
        GetBonusAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);

        var accounts = await _accountRepository.SearchAsync(
            organizationId,
            request.ClientId,
            request.IsActive,
            cancellationToken);

        return accounts.Select(account => account.ToResponse()).ToList();
    }
}
