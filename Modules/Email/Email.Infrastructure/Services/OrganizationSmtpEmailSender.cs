using System.Net;
using System.Net.Mail;
using System.Text;
using Email.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Email.Infrastructure.Services;

public sealed class OrganizationSmtpEmailSender : IOrganizationSmtpEmailSender
{
    private readonly ILogger<OrganizationSmtpEmailSender> _logger;

    public OrganizationSmtpEmailSender(ILogger<OrganizationSmtpEmailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(OrganizationEmailMessage message, CancellationToken cancellationToken)
    {
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(message.FromEmail, message.FromName, Encoding.UTF8),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };

        mailMessage.To.Add(new MailAddress(message.ToEmail));

        using var smtpClient = new SmtpClient(message.SmtpHost, message.SmtpPort)
        {
            EnableSsl = message.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(message.Username, message.Password)
        };

        try
        {
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation(
                "Organization email sent. From: {From}; To: {To}; Subject: {Subject}",
                message.FromEmail,
                message.ToEmail,
                message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Organization email send failed. From: {From}; To: {To}; Subject: {Subject}",
                message.FromEmail,
                message.ToEmail,
                message.Subject);

            throw;
        }
    }
}
