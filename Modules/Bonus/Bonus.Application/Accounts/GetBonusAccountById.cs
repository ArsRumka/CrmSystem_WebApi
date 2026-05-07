using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;

namespace Bonus.Application.Accounts;

public sealed record GetBonusAccountByIdQuery(Guid Id) : IRequest<BonusAccountResponse>;

public sealed class GetBonusAccountByIdQueryValidator : AbstractValidator<GetBonusAccountByIdQuery>
{
    public GetBonusAccountByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetBonusAccountByIdQueryHandler : IRequestHandler<GetBonusAccountByIdQuery, BonusAccountResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusAccountRepository _accountRepository;

    public GetBonusAccountByIdQueryHandler(
        ICurrentUserService currentUserService,
        IBonusAccountRepository accountRepository)
    {
        _currentUserService = currentUserService;
        _accountRepository = accountRepository;
    }

    public async Task<BonusAccountResponse> Handle(
        GetBonusAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);

        var account = await _accountRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Bonus account was not found");

        return account.ToResponse();
    }
}
