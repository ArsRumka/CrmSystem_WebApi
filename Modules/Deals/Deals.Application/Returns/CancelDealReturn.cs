using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Returns;

public sealed record CancelDealReturnCommand(Guid Id, string CancellationReason) : IRequest<DealReturnResponse>;

public sealed class CancelDealReturnCommandValidator : AbstractValidator<CancelDealReturnCommand>
{
    public CancelDealReturnCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CancellationReason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CancelDealReturnCommandHandler : IRequestHandler<CancelDealReturnCommand, DealReturnResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealReturnRepository _dealReturnRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CancelDealReturnCommandHandler(
        ICurrentUserService currentUserService,
        IDealReturnRepository dealReturnRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealReturnRepository = dealReturnRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealReturnResponse> Handle(
        CancelDealReturnCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var dealReturn = await _dealReturnRepository.GetByIdWithItemsAsync(
            organizationId,
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Deal return was not found");

        DealReturnGuards.EnsureDraft(dealReturn);

        var oldSnapshot = DealAuditSnapshots.DealReturn(dealReturn);

        dealReturn.Cancel(request.CancellationReason, userId, _dateTimeProvider.UtcNow);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Deals",
            AuditAction.Cancel,
            "DealReturn",
            dealReturn.Id,
            $"Deal return {dealReturn.Id} was cancelled",
            oldSnapshot,
            DealAuditSnapshots.DealReturn(dealReturn),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dealReturn.ToResponse();
    }
}
