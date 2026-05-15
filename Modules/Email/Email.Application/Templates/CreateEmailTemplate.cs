using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using Email.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Email.Application.Templates;

public sealed record CreateEmailTemplateCommand(
    string Name,
    string Subject,
    string Body,
    bool IsHtml) : IRequest<EmailTemplateResponse>;

public sealed class CreateEmailTemplateCommandValidator : AbstractValidator<CreateEmailTemplateCommand>
{
    public CreateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10000);
    }
}

public sealed class CreateEmailTemplateCommandHandler
    : IRequestHandler<CreateEmailTemplateCommand, EmailTemplateResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmailTemplateCommandHandler(
        ICurrentUserService currentUserService,
        IEmailTemplateRepository templateRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _templateRepository = templateRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailTemplateResponse> Handle(
        CreateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var template = new EmailTemplate(
            Guid.NewGuid(),
            organizationId,
            request.Name,
            request.Subject,
            request.Body,
            request.IsHtml,
            userId,
            _dateTimeProvider.UtcNow);

        await _templateRepository.AddAsync(template, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Email",
            AuditAction.Create,
            "EmailTemplate",
            template.Id,
            $"Email template {template.Name} was created",
            oldValues: null,
            newValues: EmailAuditSnapshots.Template(template),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template.ToResponse();
    }
}
