using Bonus.Application.Abstractions.Lookups;
using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using Bonus.Domain.Entities;
using Bonus.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;

namespace Bonus.Application.Accounts;

public sealed record AdjustBonusAccountCommand(
    Guid ClientId,
    decimal PointsDelta,
    string Reason) : IRequest<BonusAccountResponse>;

public sealed class AdjustBonusAccountCommandValidator : AbstractValidator<AdjustBonusAccountCommand>
{
    public AdjustBonusAccountCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.PointsDelta).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class AdjustBonusAccountCommandHandler
    : IRequestHandler<AdjustBonusAccountCommand, BonusAccountResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusClientLookupService _clientLookupService;
    private readonly IBonusSettingsRepository _settingsRepository;
    private readonly IBonusAccountRepository _accountRepository;
    private readonly IBonusTransactionRepository _transactionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustBonusAccountCommandHandler(
        ICurrentUserService currentUserService,
        IBonusClientLookupService clientLookupService,
        IBonusSettingsRepository settingsRepository,
        IBonusAccountRepository accountRepository,
        IBonusTransactionRepository transactionRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _clientLookupService = clientLookupService;
        _settingsRepository = settingsRepository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<BonusAccountResponse> Handle(
        AdjustBonusAccountCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);

        if (!await _clientLookupService.ExistsAsync(organizationId, request.ClientId, cancellationToken))
        {
            throw new NotFoundException("Client was not found");
        }

        var now = _dateTimeProvider.UtcNow;
        var account = await _accountRepository.GetByClientIdAsync(organizationId, request.ClientId, cancellationToken);
        if (account is null)
        {
            account = new BonusAccount(Guid.NewGuid(), organizationId, request.ClientId, now);
            await _accountRepository.AddAsync(account, cancellationToken);
        }

        var points = BonusRounding.RoundPoints(Math.Abs(request.PointsDelta));
        if (points <= 0)
        {
            throw new ConflictException("Points delta is too small");
        }

        var pointValue = await GetPointValueAsync(organizationId, cancellationToken);
        var balanceBefore = account.Balance;
        var transactionType = request.PointsDelta > 0
            ? BonusTransactionType.CorrectionIncrease
            : BonusTransactionType.CorrectionDecrease;

        if (request.PointsDelta > 0)
        {
            account.Increase(points, now);
        }
        else
        {
            if (account.Balance < points)
            {
                throw new ConflictException("Bonus balance cannot become negative");
            }

            account.Decrease(points, now);
        }

        await _transactionRepository.AddAsync(
            new BonusTransaction(
                Guid.NewGuid(),
                organizationId,
                account.Id,
                account.ClientId,
                dealId: null,
                transactionType,
                points,
                BonusRounding.RoundMoney(points * pointValue),
                pointValue,
                balanceBefore,
                account.Balance,
                request.Reason,
                now,
                userId),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account.ToResponse();
    }

    private async Task<decimal> GetPointValueAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        return settings?.PointValue ?? 1.00m;
    }
}
