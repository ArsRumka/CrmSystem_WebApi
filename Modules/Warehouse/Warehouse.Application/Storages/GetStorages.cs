using BuildingBlocks.Application.Abstractions.Auth;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;

namespace Warehouse.Application.Storages;

public sealed record GetStoragesQuery(string? Search, bool? IsActive) : IRequest<IReadOnlyList<StorageResponse>>;

public sealed class GetStoragesQueryValidator : AbstractValidator<GetStoragesQuery>
{
    public GetStoragesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class GetStoragesQueryHandler : IRequestHandler<GetStoragesQuery, IReadOnlyList<StorageResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;

    public GetStoragesQueryHandler(
        ICurrentUserService currentUserService,
        IStorageRepository storageRepository)
    {
        _currentUserService = currentUserService;
        _storageRepository = storageRepository;
    }

    public async Task<IReadOnlyList<StorageResponse>> Handle(
        GetStoragesQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var storages = await _storageRepository.SearchAsync(
            organizationId,
            request.Search,
            request.IsActive,
            cancellationToken);

        return storages.Select(storage => storage.ToResponse()).ToList();
    }
}

