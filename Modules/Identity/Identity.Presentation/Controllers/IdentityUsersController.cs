using Identity.Application.Users;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/identity")]
public sealed class IdentityUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetCurrentUserQuery(), cancellationToken));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Users", PermissionAction.Read)]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetUsersQuery(), cancellationToken));
    }

    [RequirePermission("Users", PermissionAction.Create)]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Users", PermissionAction.Update)]
    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        [FromBody] ChangeUserRoleBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ChangeUserRoleCommand(id, body.RoleId), cancellationToken));
    }

    [RequirePermission("Users", PermissionAction.Delete)]
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new DeactivateUserCommand(id), cancellationToken));
    }

    public sealed record ChangeUserRoleBody(Guid RoleId);
}
