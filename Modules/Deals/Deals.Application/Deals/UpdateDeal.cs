using Bonus.Application.Abstractions.Services;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Lookups;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Deals;

public sealed record UpdateDealCommand(
    Guid Id,
    Guid ClientId,
    Guid ResponsibleUserId,
    decimal BonusPointsUsed,
    string? Notes,
    IReadOnlyList<DealItemRequest> Items) : IRequest<DealResponse>;

public sealed class UpdateDealCommandValidator : AbstractValidator<UpdateDealCommand>
{
    public UpdateDealCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.ResponsibleUserId).NotEmpty();
        RuleFor(x => x.BonusPointsUsed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new DealItemRequestValidator());
    }
}

public sealed class UpdateDealCommandHandler : IRequestHandler<UpdateDealCommand, DealResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IClientLookupService _clientLookupService;
    private readonly IUserLookupService _userLookupService;
    private readonly ICatalogLookupService _catalogLookupService;
    private readonly IBonusDealDiscountService _bonusDealDiscountService;
    private readonly DealCalculationService _calculationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDealCommandHandler(
        ICurrentUserService currentUserService,
        IDealRepository dealRepository,
        IDealStageRepository dealStageRepository,
        IClientLookupService clientLookupService,
        IUserLookupService userLookupService,
        ICatalogLookupService catalogLookupService,
        IBonusDealDiscountService bonusDealDiscountService,
        DealCalculationService calculationService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealRepository = dealRepository;
        _dealStageRepository = dealStageRepository;
        _clientLookupService = clientLookupService;
        _userLookupService = userLookupService;
        _catalogLookupService = catalogLookupService;
        _bonusDealDiscountService = bonusDealDiscountService;
        _calculationService = calculationService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealResponse> Handle(UpdateDealCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var deal = await _dealRepository.GetByIdWithItemsAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var currentStage = await _dealStageRepository.GetByIdAsync(organizationId, deal.StageId, cancellationToken)
            ?? throw new ConflictException("Current deal stage was not found");

        if (currentStage.IsFinal)
        {
            throw new ConflictException("Final-stage deals cannot be updated");
        }

        if (!await _clientLookupService.ExistsActiveAsync(organizationId, request.ClientId, cancellationToken))
        {
            throw new NotFoundException("Client was not found");
        }

        if (!await _userLookupService.ExistsActiveAsync(organizationId, request.ResponsibleUserId, cancellationToken))
        {
            throw new NotFoundException("Responsible user was not found");
        }

        var builtItems = await DealItemFactory.BuildAsync(
            organizationId,
            deal.Id,
            request.Items,
            _catalogLookupService,
            _calculationService,
            cancellationToken);

        var preBonusCalculation = _calculationService.CalculateDeal(
            builtItems.Calculations,
            bonusPointsUsed: 0,
            bonusDiscountAmount: 0);

        var bonusDiscount = await _bonusDealDiscountService.CalculateAsync(
            organizationId,
            request.ClientId,
            preBonusCalculation.FinalAmount,
            request.BonusPointsUsed,
            cancellationToken);

        var dealCalculation = _calculationService.CalculateDeal(
            builtItems.Calculations,
            bonusDiscount.AppliedBonusPoints,
            bonusDiscount.BonusDiscountAmount);

        deal.UpdateDetails(
            request.ClientId,
            request.ResponsibleUserId,
            dealCalculation.TotalAmount,
            dealCalculation.DiscountAmount,
            dealCalculation.BonusPointsUsed,
            dealCalculation.BonusDiscountAmount,
            dealCalculation.FinalAmount,
            request.Notes,
            _dateTimeProvider.UtcNow);

        deal.ReplaceItems(builtItems.Items);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deal.ToResponse(currentStage.Name);
    }
}
