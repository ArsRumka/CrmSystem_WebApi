using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Products;

public sealed record DeactivateProductCommand(Guid Id) : IRequest;

public sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductCommandHandler(
        ICurrentUserService currentUserService,
        IProductRepository productRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _productRepository = productRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var product = await _productRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found");

        var oldValues = new
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
        };

        product.Deactivate(_dateTimeProvider.UtcNow);
        await _auditLogService.LogAsync(
            organizationId,
            _currentUserService.UserId,
            "Catalog",
            AuditAction.Deactivate,
            "Product",
            product.Id,
            $"Product {product.Name} was deactivated",
            oldValues,
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
    }
}
