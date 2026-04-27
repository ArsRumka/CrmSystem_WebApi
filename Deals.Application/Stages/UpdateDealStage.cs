using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Stages;

public sealed record UpdateDealStageCommand(
    Guid Id,
    string Name,
    int Order,
    bool IsFinal,
    bool IsSuccessful) : IRequest<DealStageResponse>;

public sealed class UpdateDealStageCommandValidator : AbstractValidator<UpdateDealStageCommand>
{
    public UpdateDealStageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}

public sealed class UpdateDealStageCommandHandler : IRequestHandler<UpdateDealStageCommand, DealStageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDealStageCommandHandler(
        ICurrentUserService currentUserService,
        IDealStageRepository dealStageRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealStageRepository = dealStageRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealStageResponse> Handle(
        UpdateDealStageCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var stage = await _dealStageRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Deal stage was not found");

        stage.Update(
            request.Name,
            request.Order,
            request.IsFinal,
            request.IsSuccessful,
            _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stage.ToResponse();
    }
}
