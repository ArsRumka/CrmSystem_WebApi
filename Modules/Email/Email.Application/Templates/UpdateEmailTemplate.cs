using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Email.Application.Templates;

public sealed record UpdateEmailTemplateCommand(
    Guid Id,
    string Name,
    string Subject,
    string Body,
    bool IsHtml,
    bool IsActive) : IRequest<EmailTemplateResponse>;

public sealed class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10000);
    }
}

public sealed class UpdateEmailTemplateCommandHandler
    : IRequestHandler<UpdateEmailTemplateCommand, EmailTemplateResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailTemplateCommandHandler(
        ICurrentUserService currentUserService,
        IEmailTemplateRepository templateRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _templateRepository = templateRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailTemplateResponse> Handle(
        UpdateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var template = await _templateRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Email template was not found");

        template.Update(
            request.Name,
            request.Subject,
            request.Body,
            request.IsHtml,
            request.IsActive,
            _dateTimeProvider.UtcNow,
            userId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template.ToResponse();
    }
}
