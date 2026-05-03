using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;

namespace Warehouse.Application.Storages;

public sealed record GetStorageByIdQuery(Guid Id) : IRequest<StorageResponse>;

public sealed class GetStorageByIdQueryValidator : AbstractValidator<GetStorageByIdQuery>
{
    public GetStorageByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetStorageByIdQueryHandler : IRequestHandler<GetStorageByIdQuery, StorageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;

    public GetStorageByIdQueryHandler(
        ICurrentUserService currentUserService,
        IStorageRepository storageRepository)
    {
        _currentUserService = currentUserService;
        _storageRepository = storageRepository;
    }

    public async Task<StorageResponse> Handle(GetStorageByIdQuery request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var storage = await _storageRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Storage was not found");

        return storage.ToResponse();
    }
}

