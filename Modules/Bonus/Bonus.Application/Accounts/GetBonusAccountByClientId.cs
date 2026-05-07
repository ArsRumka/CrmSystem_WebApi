using Bonus.Application.Abstractions.Lookups;
using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using Bonus.Domain.Entities;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;

namespace Bonus.Application.Accounts;

public sealed record GetBonusAccountByClientIdQuery(Guid ClientId) : IRequest<BonusAccountResponse>;

public sealed class GetBonusAccountByClientIdQueryValidator : AbstractValidator<GetBonusAccountByClientIdQuery>
{
    public GetBonusAccountByClientIdQueryValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
    }
}

public sealed class GetBonusAccountByClientIdQueryHandler
    : IRequestHandler<GetBonusAccountByClientIdQuery, BonusAccountResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusClientLookupService _clientLookupService;
    private readonly IBonusAccountRepository _accountRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public GetBonusAccountByClientIdQueryHandler(
        ICurrentUserService currentUserService,
        IBonusClientLookupService clientLookupService,
        IBonusAccountRepository accountRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _clientLookupService = clientLookupService;
        _accountRepository = accountRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<BonusAccountResponse> Handle(
        GetBonusAccountByClientIdQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);

        if (!await _clientLookupService.ExistsAsync(organizationId, request.ClientId, cancellationToken))
        {
            throw new NotFoundException("Client was not found");
        }

        var account = await _accountRepository.GetByClientIdAsync(organizationId, request.ClientId, cancellationToken);
        if (account is null)
        {
            account = new BonusAccount(Guid.NewGuid(), organizationId, request.ClientId, _dateTimeProvider.UtcNow);
            await _accountRepository.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return account.ToResponse();
    }
}
