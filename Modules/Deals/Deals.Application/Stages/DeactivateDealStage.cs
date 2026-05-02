using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using FluentValidation;
using MediatR;

namespace Deals.Application.Stages;

public sealed record DeactivateDealStageCommand(Guid Id) : IRequest;

public sealed class DeactivateDealStageCommandValidator : AbstractValidator<DeactivateDealStageCommand>
{
    public DeactivateDealStageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateDealStageCommandHandler : IRequestHandler<DeactivateDealStageCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateDealStageCommandHandler(
        ICurrentUserService currentUserService,
        IDealStageRepository dealStageRepository,
        IDealRepository dealRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealStageRepository = dealStageRepository;
        _dealRepository = dealRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateDealStageCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var stage = await _dealStageRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Deal stage was not found");

        if (await _dealRepository.ExistsActiveByStageIdAsync(organizationId, request.Id, cancellationToken))
        {
            throw new ConflictException("Deal stage is used by active deals");
        }

        stage.Deactivate(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
