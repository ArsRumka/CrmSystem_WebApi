using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Products;

public sealed record CreateProductCommand(
    Guid? CategoryId,
    string Name,
    string? Sku,
    string? Description,
    decimal Price,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue) : IRequest<ProductResponse>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
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

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        if (request.CategoryId.HasValue &&
            !await _categoryRepository.ExistsByIdAsync(organizationId, request.CategoryId.Value, cancellationToken))
        {
            throw new NotFoundException("Category was not found");
        }

        var product = new Product(
            Guid.NewGuid(),
            organizationId,
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

        await _productRepository.AddAsync(product, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            _currentUserService.UserId,
            "Catalog",
            AuditAction.Create,
            "Product",
            product.Id,
            $"Product {product.Name} was created",
            oldValues: null,
            newValues: new
            {
                product.Name,
                product.CategoryId,
                product.Sku,
                product.Price,
                product.BonusType,
                product.BonusValue,
                product.DiscountType,
                product.DiscountValue,
                product.IsActive
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }
}
