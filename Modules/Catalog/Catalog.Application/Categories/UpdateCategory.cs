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

namespace Catalog.Application.Categories;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue) : IRequest<CategoryResponse>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ParentCategoryId).NotEqual(Guid.Empty).When(x => x.ParentCategoryId.HasValue);
        RuleFor(x => x)
            .Must(x => !x.ParentCategoryId.HasValue || x.ParentCategoryId.Value != x.Id)
            .WithMessage("Parent category cannot be the category itself");
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

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var category = await _categoryRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Category was not found");

        if (request.ParentCategoryId.HasValue &&
            !await _categoryRepository.ExistsByIdAsync(organizationId, request.ParentCategoryId.Value, cancellationToken))
        {
            throw new NotFoundException("Parent category was not found");
        }

        if (await _categoryRepository.WouldCreateCycleAsync(
                organizationId,
                request.Id,
                request.ParentCategoryId,
                cancellationToken))
        {
            throw new ConflictException("Category parent cycle is not allowed");
        }

        category.Update(
            request.Name,
            request.ParentCategoryId,
            request.BonusType,
            request.BonusValue,
            request.DiscountType,
            request.DiscountValue,
            _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToResponse();
    }
}
