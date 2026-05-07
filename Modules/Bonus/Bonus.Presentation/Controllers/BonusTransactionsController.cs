using Bonus.Application.Transactions;
using Bonus.Domain.Enums;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bonus.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/bonus/transactions")]
public sealed class BonusTransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BonusTransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Bonus", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetBonusTransactions(
        [FromQuery] Guid? bonusAccountId,
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? dealId,
        [FromQuery] BonusTransactionType? type,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetBonusTransactionsQuery(bonusAccountId, clientId, dealId, type, dateFrom, dateTo),
            cancellationToken));
    }

    [RequirePermission("Bonus", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBonusTransactionById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetBonusTransactionByIdQuery(id), cancellationToken));
    }
}
