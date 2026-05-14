using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Abstractions.Services;
using Email.Application.Common;
using Email.Application.Contracts;
using Email.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Email.Application.Settings;

public sealed record UpdateEmailSettingsCommand(
    string SenderName,
    string SenderEmail,
    string SmtpHost,
    int SmtpPort,
    bool UseSsl,
    string Username,
    string? SmtpPassword,
    bool IsEnabled) : IRequest<EmailSettingsResponse>;

public sealed class UpdateEmailSettingsCommandValidator : AbstractValidator<UpdateEmailSettingsCommand>
{
    public UpdateEmailSettingsCommandValidator()
    {
        RuleFor(x => x.SenderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SenderEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.SmtpHost).NotEmpty().MaximumLength(300);
        RuleFor(x => x.SmtpPort).InclusiveBetween(1, 65535);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(320);
    }
}

public sealed class UpdateEmailSettingsCommandHandler
    : IRequestHandler<UpdateEmailSettingsCommand, EmailSettingsResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailSettingsRepository _settingsRepository;
    private readonly IEmailPasswordProtector _passwordProtector;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmailSettingsCommandHandler(
        ICurrentUserService currentUserService,
        IEmailSettingsRepository settingsRepository,
        IEmailPasswordProtector passwordProtector,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _settingsRepository = settingsRepository;
        _passwordProtector = passwordProtector;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailSettingsResponse> Handle(
        UpdateEmailSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);
        var now = _dateTimeProvider.UtcNow;
        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);

        var passwordEncrypted = !string.IsNullOrWhiteSpace(request.SmtpPassword)
            ? _passwordProtector.Protect(request.SmtpPassword)
            : settings?.PasswordEncrypted;

        if (string.IsNullOrWhiteSpace(passwordEncrypted))
        {
            throw new ConflictException("SMTP password is required when creating email settings");
        }

        if (settings is null)
        {
            settings = new EmailSettings(
                Guid.NewGuid(),
                organizationId,
                request.SenderName,
                request.SenderEmail,
                request.SmtpHost,
                request.SmtpPort,
                request.UseSsl,
                request.Username,
                passwordEncrypted,
                request.IsEnabled,
                now);

            await _settingsRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.Update(
                request.SenderName,
                request.SenderEmail,
                request.SmtpHost,
                request.SmtpPort,
                request.UseSsl,
                request.Username,
                passwordEncrypted,
                request.IsEnabled,
                now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settings.ToResponse();
    }
}
