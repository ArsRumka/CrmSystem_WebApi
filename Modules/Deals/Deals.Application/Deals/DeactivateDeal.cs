using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using FluentValidation;
using MediatR;

namespace Deals.Application.Deals;

public sealed record DeactivateDealCommand(Guid Id) : IRequest;

public sealed class DeactivateDealCommandValidator : AbstractValidator<DeactivateDealCommand>
{
    public DeactivateDealCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateDealCommandHandler : IRequestHandler<DeactivateDealCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateDealCommandHandler(
        ICurrentUserService currentUserService,
        IDealRepository dealRepository,
        IDealStageRepository dealStageRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealRepository = dealRepository;
        _dealStageRepository = dealStageRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateDealCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var deal = await _dealRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var currentStage = await _dealStageRepository.GetByIdAsync(organizationId, deal.StageId, cancellationToken)
            ?? throw new ConflictException("Current deal stage was not found");

        if (currentStage.IsFinal)
        {
            throw new ConflictException("Final-stage deals cannot be deactivated");
        }

        deal.Deactivate(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
