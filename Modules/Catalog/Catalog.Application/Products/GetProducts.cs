using BuildingBlocks.Application.Abstractions.Auth;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using Catalog.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Products;

public sealed record GetProductsQuery(
    string? Search,
    Guid? CategoryId,
    bool? IsActive,
    BonusType? BonusType,
    DiscountType? DiscountType) : IRequest<IReadOnlyList<ProductResponse>>;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.BonusType).IsInEnum().When(x => x.BonusType.HasValue);
        RuleFor(x => x.DiscountType).IsInEnum().When(x => x.DiscountType.HasValue);
    }
}

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(
        ICurrentUserService currentUserService,
        IProductRepository productRepository)
    {
        _currentUserService = currentUserService;
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductResponse>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var products = await _productRepository.SearchAsync(
            organizationId,
            request.Search,
            request.CategoryId,
            request.IsActive,
            request.BonusType,
            request.DiscountType,
            cancellationToken);

        return products
            .Select(product => product.ToResponse())
            .ToList();
    }
}
