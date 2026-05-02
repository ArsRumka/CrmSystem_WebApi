using BuildingBlocks.Application.Abstractions.Auth;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Categories;

public sealed record GetCategoriesQuery(
    string? Search,
    Guid? ParentCategoryId,
    bool? IsActive) : IRequest<IReadOnlyList<CategoryResponse>>;

public sealed class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.ParentCategoryId).NotEqual(Guid.Empty).When(x => x.ParentCategoryId.HasValue);
    }
}

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesQueryHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryResponse>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var categories = await _categoryRepository.SearchAsync(
            organizationId,
            request.Search,
            request.ParentCategoryId,
            request.IsActive,
            cancellationToken);

        return categories
            .Select(category => category.ToResponse())
            .ToList();
    }
}
