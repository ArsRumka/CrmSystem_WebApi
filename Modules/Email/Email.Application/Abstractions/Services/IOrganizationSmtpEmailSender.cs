namespace Email.Application.Abstractions.Services;

public interface IOrganizationSmtpEmailSender
{
    Task SendAsync(OrganizationEmailMessage message, CancellationToken cancellationToken);
}

public sealed record OrganizationEmailMessage(
    string FromName,
    string FromEmail,
    string ToEmail,
    string Subject,
    string Body,
    bool IsHtml,
    string SmtpHost,
    int SmtpPort,
    bool UseSsl,
    string Username,
    string Password);
