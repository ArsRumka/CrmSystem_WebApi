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
using Warehouse.Application.Contracts;

namespace Warehouse.Application.Storages;

public sealed record UpdateStorageCommand(
    Guid Id,
    string Name,
    string? Address) : IRequest<StorageResponse>;

public sealed class UpdateStorageCommandValidator : AbstractValidator<UpdateStorageCommand>
{
    public UpdateStorageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public sealed class UpdateStorageCommandHandler : IRequestHandler<UpdateStorageCommand, StorageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStorageCommandHandler(
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

    public async Task<StorageResponse> Handle(UpdateStorageCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var storage = await _storageRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Storage was not found");

        var oldSnapshot = WarehouseAuditSnapshots.Storage(storage);

        storage.Update(request.Name, request.Address, _dateTimeProvider.UtcNow);

        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Warehouse",
            AuditAction.Update,
            "Storage",
            storage.Id,
            $"Storage {storage.Name} was updated",
            oldSnapshot,
            WarehouseAuditSnapshots.Storage(storage),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return storage.ToResponse();
    }
}
