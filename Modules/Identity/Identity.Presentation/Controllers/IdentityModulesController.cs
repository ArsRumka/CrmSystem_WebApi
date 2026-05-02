using Identity.Application.Modules;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/identity/modules")]
public sealed class IdentityModulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityModulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetModules(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetModulesQuery(), cancellationToken));
    }
}
