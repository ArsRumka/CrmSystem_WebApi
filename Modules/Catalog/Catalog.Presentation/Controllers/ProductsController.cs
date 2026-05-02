using Catalog.Application.Products;
using Catalog.Domain.Enums;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/catalog/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Catalog", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] BonusType? bonusType,
        [FromQuery] DiscountType? discountType,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetProductsQuery(search, categoryId, isActive, bonusType, discountType),
            cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetProductByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id,
            body.CategoryId,
            body.Name,
            body.Sku,
            body.Description,
            body.Price,
            body.BonusType,
            body.BonusValue,
            body.DiscountType,
            body.DiscountValue);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateProductCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateProductBody(
        Guid? CategoryId,
        string Name,
        string? Sku,
        string? Description,
        decimal Price,
        BonusType BonusType,
        decimal? BonusValue,
        DiscountType DiscountType,
        decimal? DiscountValue);
}
