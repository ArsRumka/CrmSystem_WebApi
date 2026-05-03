using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Storages;

namespace Warehouse.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/warehouse/storages")]
public sealed class StoragesController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoragesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Warehouse", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetStorages(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetStoragesQuery(search, isActive), cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStorageById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetStorageByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateStorage(
        [FromBody] CreateStorageCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStorage(
        Guid id,
        [FromBody] UpdateStorageBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new UpdateStorageCommand(id, body.Name, body.Address),
            cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Update)]
    [HttpPut("{id:guid}/make-default")]
    public async Task<IActionResult> MakeStorageDefault(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new MakeStorageDefaultCommand(id), cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateStorage(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateStorageCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateStorageBody(string Name, string? Address);
}

