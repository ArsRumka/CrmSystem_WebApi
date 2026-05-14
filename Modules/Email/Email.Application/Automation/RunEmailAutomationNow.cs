using BuildingBlocks.Application.Abstractions.Auth;
using Email.Application.Abstractions.Services;
using Email.Application.Common;
using Email.Application.Contracts;
using MediatR;

namespace Email.Application.Automation;

public sealed record RunEmailAutomationNowCommand : IRequest<EmailAutomationRunResponse>;

public sealed class RunEmailAutomationNowCommandHandler
    : IRequestHandler<RunEmailAutomationNowCommand, EmailAutomationRunResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailAutomationRunner _automationRunner;

    public RunEmailAutomationNowCommandHandler(
        ICurrentUserService currentUserService,
        IEmailAutomationRunner automationRunner)
    {
        _currentUserService = currentUserService;
        _automationRunner = automationRunner;
    }

    public Task<EmailAutomationRunResponse> Handle(
        RunEmailAutomationNowCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        return _automationRunner.RunForOrganizationAsync(organizationId, cancellationToken);
    }
}
