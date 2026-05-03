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

public sealed record MakeStorageDefaultCommand(Guid Id) : IRequest<StorageResponse>;

public sealed class MakeStorageDefaultCommandValidator : AbstractValidator<MakeStorageDefaultCommand>
{
    public MakeStorageDefaultCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class MakeStorageDefaultCommandHandler : IRequestHandler<MakeStorageDefaultCommand, StorageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MakeStorageDefaultCommandHandler(
        ICurrentUserService currentUserService,
        IStorageRepository storageRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _storageRepository = storageRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<StorageResponse> Handle(MakeStorageDefaultCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var storage = await _storageRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Storage was not found");

        if (!storage.IsActive)
        {
            throw new ConflictException("Inactive storage cannot be default");
        }

        var now = _dateTimeProvider.UtcNow;

        await _storageRepository.ClearDefaultFlagsAsync(organizationId, storage.Id, now, cancellationToken);
        storage.MakeDefault(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return storage.ToResponse();
    }
}

