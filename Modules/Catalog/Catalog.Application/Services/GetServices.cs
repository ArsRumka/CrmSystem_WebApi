using BuildingBlocks.Application.Abstractions.Auth;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using Catalog.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Services;

public sealed record GetServicesQuery(
    string? Search,
    Guid? CategoryId,
    bool? IsActive,
    BonusType? BonusType,
    DiscountType? DiscountType) : IRequest<IReadOnlyList<ServiceResponse>>;

public sealed class GetServicesQueryValidator : AbstractValidator<GetServicesQuery>
{
    public GetServicesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.BonusType).IsInEnum().When(x => x.BonusType.HasValue);
        RuleFor(x => x.DiscountType).IsInEnum().When(x => x.DiscountType.HasValue);
    }
}

public sealed class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, IReadOnlyList<ServiceResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceRepository _serviceRepository;

    public GetServicesQueryHandler(
        ICurrentUserService currentUserService,
        IServiceRepository serviceRepository)
    {
        _currentUserService = currentUserService;
        _serviceRepository = serviceRepository;
    }

    public async Task<IReadOnlyList<ServiceResponse>> Handle(
        GetServicesQuery request,
        CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var services = await _serviceRepository.SearchAsync(
            organizationId,
            request.Search,
            request.CategoryId,
            request.IsActive,
            request.BonusType,
            request.DiscountType,
            cancellationToken);

        return services
            .Select(service => service.ToResponse())
            .ToList();
    }
}
