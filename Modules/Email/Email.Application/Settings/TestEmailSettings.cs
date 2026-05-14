using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Abstractions.Services;
using Email.Application.Common;
using FluentValidation;
using MediatR;

namespace Email.Application.Settings;

public sealed record TestEmailSettingsCommand(string? TestToEmail) : IRequest;

public sealed class TestEmailSettingsCommandValidator : AbstractValidator<TestEmailSettingsCommand>
{
    public TestEmailSettingsCommandValidator()
    {
        RuleFor(x => x.TestToEmail)
            .EmailAddress()
            .MaximumLength(320)
            .When(x => !string.IsNullOrWhiteSpace(x.TestToEmail));
    }
}

public sealed class TestEmailSettingsCommandHandler : IRequestHandler<TestEmailSettingsCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailSettingsRepository _settingsRepository;
    private readonly IEmailPasswordProtector _passwordProtector;
    private readonly IOrganizationSmtpEmailSender _smtpEmailSender;

    public TestEmailSettingsCommandHandler(
        ICurrentUserService currentUserService,
        IEmailSettingsRepository settingsRepository,
        IEmailPasswordProtector passwordProtector,
        IOrganizationSmtpEmailSender smtpEmailSender)
    {
        _currentUserService = currentUserService;
        _settingsRepository = settingsRepository;
        _passwordProtector = passwordProtector;
        _smtpEmailSender = smtpEmailSender;
    }

    public async Task Handle(TestEmailSettingsCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken)
            ?? throw new ConflictException("Email settings are not configured");

        var toEmail = string.IsNullOrWhiteSpace(request.TestToEmail)
            ? settings.SenderEmail
            : request.TestToEmail.Trim();

        try
        {
            await _smtpEmailSender.SendAsync(
                new OrganizationEmailMessage(
                    settings.SenderName,
                    settings.SenderEmail,
                    toEmail,
                    "CRM SMTP settings test",
                    "This is a test email from CRM SMTP settings.",
                    IsHtml: false,
                    settings.SmtpHost,
                    settings.SmtpPort,
                    settings.UseSsl,
                    settings.Username,
                    _passwordProtector.Unprotect(settings.PasswordEncrypted)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ConflictException($"SMTP test failed: {ex.Message}");
        }
    }
}
