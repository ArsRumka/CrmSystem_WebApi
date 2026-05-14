using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using Email.Domain.Entities;
using MediatR;

namespace Email.Application.Automation;

public sealed record GetEmailAutomationRuleQuery : IRequest<EmailAutomationRuleResponse>;

public sealed class GetEmailAutomationRuleQueryHandler
    : IRequestHandler<GetEmailAutomationRuleQuery, EmailAutomationRuleResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailAutomationRuleRepository _ruleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public GetEmailAutomationRuleQueryHandler(
        ICurrentUserService currentUserService,
        IEmailAutomationRuleRepository ruleRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _ruleRepository = ruleRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailAutomationRuleResponse> Handle(
        GetEmailAutomationRuleQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var rule = await _ruleRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        if (rule is null)
        {
            rule = EmailAutomationRule.CreateDefault(organizationId, _dateTimeProvider.UtcNow);
            await _ruleRepository.AddAsync(rule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return rule.ToResponse();
    }
}
