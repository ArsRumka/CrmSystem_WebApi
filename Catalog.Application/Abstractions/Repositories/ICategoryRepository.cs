using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions.Repositories;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task<Category?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<Category>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? parentCategoryId,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<bool> WouldCreateCycleAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? newParentCategoryId,
        CancellationToken cancellationToken);
}
