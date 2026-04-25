using System.Net;
using System.Net.Mail;
using System.Text;
using BuildingBlocks.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public sealed class EmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (_options.UseConsole)
        {
            _logger.LogInformation(
                "Console email. From: {From}; To: {To}; Subject: {Subject}; Body: {Body}",
                _options.From,
                to,
                subject,
                body);

            return;
        }

        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.From))
        {
            throw new InvalidOperationException("Email sender address is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, _options.DisplayName, Encoding.UTF8),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };

        message.To.Add(new MailAddress(to));

        using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.Username,
                _options.Password);
        }

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation(
                "Email sent. From: {From}; To: {To}; Subject: {Subject}",
                _options.From,
                to,
                subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email. From: {From}; To: {To}; Subject: {Subject}",
                _options.From,
                to,
                subject);

            throw;
        }
    }
}