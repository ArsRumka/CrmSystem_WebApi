using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using Catalog.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Products;

public sealed record UpdateProductCommand(
    Guid Id,
    Guid? CategoryId,
    string Name,
    string? Sku,
    string? Description,
    decimal Price,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue) : IRequest<ProductResponse>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BonusType).IsInEnum();
        RuleFor(x => x.DiscountType).IsInEnum();
        RuleFor(x => x)
            .Must(x => CatalogValidationRules.IsValidBonusRule(x.BonusType, x.BonusValue))
            .WithMessage("Invalid bonus rule");
        RuleFor(x => x)
            .Must(x => CatalogValidationRules.IsValidDiscountRule(x.DiscountType, x.DiscountValue))
            .WithMessage("Invalid discount rule");
    }
}

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var product = await _productRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found");

        if (request.CategoryId.HasValue &&
            !await _categoryRepository.ExistsByIdAsync(organizationId, request.CategoryId.Value, cancellationToken))
        {
            throw new NotFoundException("Category was not found");
        }

        product.Update(
            request.CategoryId,
            request.Name,
            request.Sku,
            request.Description,
            request.Price,
            request.BonusType,
            request.BonusValue,
            request.DiscountType,
            request.DiscountValue,
            _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }
}
