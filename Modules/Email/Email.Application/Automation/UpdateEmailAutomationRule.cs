using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using Email.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Email.Application.Automation;

public sealed record UpdateEmailAutomationRuleCommand(
    bool IsEnabled,
    Guid? TemplateId,
    int InactivityDays,
    int RepeatAfterDays) : IRequest<EmailAutomationRuleResponse>;

public sealed class UpdateEmailAutomationRuleCommandValidator
    : AbstractValidator<UpdateEmailAutomationRuleCommand>
{
    public UpdateEmailAutomationRuleCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEqual(Guid.Empty).When(x => x.TemplateId.HasValue);
        RuleFor(x => x.TemplateId)
            .NotNull()
            .When(x => x.IsEnabled)
            .WithMessage("TemplateId is required when automation is enabled");
        RuleFor(x => x.InactivityDays).GreaterThanOrEqualTo(1);
        RuleFor(x => x.RepeatAfterDays).GreaterThanOrEqualTo(1);
    }
}

public sealed class UpdateEmailAutomationRuleCommandHandler
    : IRequestHandler<UpdateEmailAutomationRuleCommand, EmailAutomationRuleResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailAutomationRuleRepository _ruleRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailAutomationRuleCommandHandler(
        ICurrentUserService currentUserService,
        IEmailAutomationRuleRepository ruleRepository,
        IEmailTemplateRepository templateRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _ruleRepository = ruleRepository;
        _templateRepository = templateRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailAutomationRuleResponse> Handle(
        UpdateEmailAutomationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        if (request.IsEnabled)
        {
            var template = await _templateRepository.GetByIdAsync(
                organizationId,
                request.TemplateId!.Value,
                cancellationToken)
                ?? throw new NotFoundException("Email template was not found");

            if (!template.IsActive)
            {
                throw new ConflictException("Email template is inactive");
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var rule = await _ruleRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        object? oldSnapshot = null;
        if (rule is null)
        {
            rule = EmailAutomationRule.CreateDefault(organizationId, now);
            await _ruleRepository.AddAsync(rule, cancellationToken);
        }
        else
        {
            oldSnapshot = EmailAuditSnapshots.AutomationRule(rule);
        }

        rule.Update(
            request.IsEnabled,
            request.TemplateId,
            request.InactivityDays,
            request.RepeatAfterDays,
            now,
            userId);

        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Email",
            AuditAction.Update,
            "EmailAutomationRule",
            rule.Id,
            "Email automation rule was updated",
            oldSnapshot,
            EmailAuditSnapshots.AutomationRule(rule),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return rule.ToResponse();
    }
}
