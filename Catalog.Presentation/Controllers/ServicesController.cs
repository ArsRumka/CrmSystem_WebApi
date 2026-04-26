using Catalog.Application.Services;
using Catalog.Domain.Enums;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/catalog/services")]
public sealed class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Catalog", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetServices(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] BonusType? bonusType,
        [FromQuery] DiscountType? discountType,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetServicesQuery(search, categoryId, isActive, bonusType, discountType),
            cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetServiceById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetServiceByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateService(
        [FromBody] CreateServiceCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Catalog", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateService(
        Guid id,
        [FromBody] UpdateServiceBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateServiceCommand(
            id,
            body.CategoryId,
            body.Name,
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
    public async Task<IActionResult> DeactivateService(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateServiceCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateServiceBody(
        Guid? CategoryId,
        string Name,
        string? Description,
        decimal Price,
        BonusType BonusType,
        decimal? BonusValue,
        DiscountType DiscountType,
        decimal? DiscountValue);
}
