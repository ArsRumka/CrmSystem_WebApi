using Clients.Application.Clients;
using Clients.Domain.Enums;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clients.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Clients", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] string? search,
        [FromQuery] ClientStatus? status,
        [FromQuery] ClientSource? source,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetClientsQuery(search, status, source, isActive), cancellationToken));
    }

    [RequirePermission("Clients", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClientById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetClientByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Clients", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateClientCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Clients", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateClient(
        Guid id,
        [FromBody] UpdateClientBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClientCommand(
            id,
            body.FirstName,
            body.LastName,
            body.MiddleName,
            body.Email,
            body.Phone,
            body.Status,
            body.Source,
            body.AllowMarketingEmails,
            body.Notes);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Clients", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateClient(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateClientCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateClientBody(
        string FirstName,
        string LastName,
        string? MiddleName,
        string? Email,
        string? Phone,
        ClientStatus Status,
        ClientSource Source,
        bool AllowMarketingEmails,
        string? Notes);
}
