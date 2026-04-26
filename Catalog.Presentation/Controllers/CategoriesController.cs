using Catalog.Application.Categories;
using Catalog.Domain.Enums;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/catalog/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Catalog", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search,
        [FromQuery] Guid? parentCategoryId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetCategoriesQuery(search, parentCategoryId, isActive), cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetCategoryByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(
            id,
            body.Name,
            body.ParentCategoryId,
            body.BonusType,
            body.BonusValue,
            body.DiscountType,
            body.DiscountValue);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateCategory(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateCategoryCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateCategoryBody(
        string Name,
        Guid? ParentCategoryId,
        BonusType BonusType,
        decimal? BonusValue,
        DiscountType DiscountType,
        decimal? DiscountValue);
}
