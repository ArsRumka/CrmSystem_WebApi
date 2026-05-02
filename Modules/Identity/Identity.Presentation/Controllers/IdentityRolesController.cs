using Identity.Application.Contracts;
using Identity.Application.Roles;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/roles")]
public sealed class IdentityRolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityRolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Roles", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetRolesQuery(), cancellationToken));
    }

    [RequirePermission("Roles", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Roles", PermissionAction.Update)]
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new UpdateRolePermissionsCommand(id, body.Permissions), cancellationToken));
    }

    [RequirePermission("Roles", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new DeleteRoleCommand(id), cancellationToken));
    }

    public sealed record UpdateRolePermissionsBody(IReadOnlyList<RolePermissionRequest> Permissions);
}
