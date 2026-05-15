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

namespace Catalog.Application.Categories;

public sealed record DeactivateCategoryCommand(Guid Id) : IRequest;

public sealed class DeactivateCategoryCommandValidator : AbstractValidator<DeactivateCategoryCommand>
{
    public DeactivateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCategoryCommandHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var category = await _categoryRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Category was not found");

        var oldValues = new
        {
            category.Name,
            category.ParentCategoryId,
            category.BonusType,
            category.BonusValue,
            category.DiscountType,
            category.DiscountValue,
            category.IsActive
        };

        category.Deactivate(_dateTimeProvider.UtcNow);
        await _auditLogService.LogAsync(
            organizationId,
            _currentUserService.UserId,
            "Catalog",
            AuditAction.Deactivate,
            "Category",
            category.Id,
            $"Category {category.Name} was deactivated",
            oldValues,
            newValues: new
            {
                category.Name,
                category.ParentCategoryId,
                category.BonusType,
                category.BonusValue,
                category.DiscountType,
                category.DiscountValue,
                category.IsActive
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
