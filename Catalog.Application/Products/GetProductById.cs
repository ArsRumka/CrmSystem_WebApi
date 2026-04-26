using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Products;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductResponse>;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(
        ICurrentUserService currentUserService,
        IProductRepository productRepository)
    {
        _currentUserService = currentUserService;
        _productRepository = productRepository;
    }

    public async Task<ProductResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var product = await _productRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found");

        return product.ToResponse();
    }
}
