using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Storages;

public sealed record CreateStorageCommand(
    string Name,
    string? Address,
    bool? IsDefault) : IRequest<StorageResponse>;

public sealed class CreateStorageCommandValidator : AbstractValidator<CreateStorageCommand>
{
    public CreateStorageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public sealed class CreateStorageCommandHandler : IRequestHandler<CreateStorageCommand, StorageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStorageCommandHandler(
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

    public async Task<StorageResponse> Handle(CreateStorageCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var now = _dateTimeProvider.UtcNow;
        var storageId = Guid.NewGuid();
        var hasStorages = await _storageRepository.AnyAsync(organizationId, cancellationToken);
        var shouldBeDefault = !hasStorages || request.IsDefault == true;

        if (shouldBeDefault)
        {
            await _storageRepository.ClearDefaultFlagsAsync(organizationId, storageId, now, cancellationToken);
        }

        var storage = new Storage(
            storageId,
            organizationId,
            request.Name,
            request.Address,
            shouldBeDefault,
            now);

        await _storageRepository.AddAsync(storage, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Warehouse",
            AuditAction.Create,
            "Storage",
            storage.Id,
            $"Storage {storage.Name} was created",
            oldValues: null,
            newValues: WarehouseAuditSnapshots.Storage(storage),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return storage.ToResponse();
    }
}
