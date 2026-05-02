using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Categories;

public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryResponse>;

public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByIdQueryHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var category = await _categoryRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Category was not found");

        return category.ToResponse();
    }
}
