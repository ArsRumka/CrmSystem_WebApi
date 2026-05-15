using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;

namespace Warehouse.Application.Storages;

public sealed record DeactivateStorageCommand(Guid Id) : IRequest;

public sealed class DeactivateStorageCommandValidator : AbstractValidator<DeactivateStorageCommand>
{
    public DeactivateStorageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateStorageCommandHandler : IRequestHandler<DeactivateStorageCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateStorageCommandHandler(
        ICurrentUserService currentUserService,
        IStorageRepository storageRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _storageRepository = storageRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateStorageCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var storage = await _storageRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Storage was not found");

        if (!storage.IsActive)
        {
            return;
        }

        if (await _storageRepository.HasPositiveStockAsync(organizationId, storage.Id, cancellationToken))
        {
            throw new ConflictException("Storage with positive stock cannot be deactivated");
        }

        var activeStorages = await _storageRepository.GetActiveAsync(organizationId, cancellationToken);
        if (activeStorages.Count <= 1)
        {
            throw new ConflictException("Last active storage cannot be deactivated");
        }

        if (storage.IsDefault && activeStorages.Any(x => x.Id != storage.Id))
        {
            throw new ConflictException("Default storage cannot be deactivated while other active storages exist");
        }

        var oldSnapshot = WarehouseAuditSnapshots.Storage(storage);

        storage.Deactivate(_dateTimeProvider.UtcNow);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Warehouse",
            AuditAction.Deactivate,
            "Storage",
            storage.Id,
            $"Storage {storage.Name} was deactivated",
            oldSnapshot,
            WarehouseAuditSnapshots.Storage(storage),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
