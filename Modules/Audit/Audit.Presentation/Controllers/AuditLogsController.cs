using Audit.Application.Logs;
using Audit.Domain.Enums;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/audit/logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Audit", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? moduleCode,
        [FromQuery] AuditAction? action,
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAuditLogsQuery(
            moduleCode,
            action,
            entityName,
            entityId,
            userId,
            dateFrom,
            dateTo,
            skip,
            take);

        return Ok(await _mediator.Send(query, cancellationToken));
    }

    [RequirePermission("Audit", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAuditLogById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetAuditLogByIdQuery(id), cancellationToken));
    }
}

