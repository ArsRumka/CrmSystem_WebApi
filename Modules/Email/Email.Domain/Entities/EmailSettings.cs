using System.Net.Mail;

namespace Email.Domain.Entities;

public class EmailSettings
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string SenderName { get; private set; } = null!;
    public string SenderEmail { get; private set; } = null!;
    public string SmtpHost { get; private set; } = null!;
    public int SmtpPort { get; private set; }
    public bool UseSsl { get; private set; }
    public string Username { get; private set; } = null!;
    public string PasswordEncrypted { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private EmailSettings()
    {
    }

    public EmailSettings(
        Guid id,
        Guid organizationId,
        string senderName,
        string senderEmail,
        string smtpHost,
        int smtpPort,
        bool useSsl,
        string username,
        string passwordEncrypted,
        bool isEnabled,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        CreatedAt = createdAt;

        SetValues(senderName, senderEmail, smtpHost, smtpPort, useSsl, username, passwordEncrypted, isEnabled);
    }

    public void Update(
        string senderName,
        string senderEmail,
        string smtpHost,
        int smtpPort,
        bool useSsl,
        string username,
        string passwordEncrypted,
        bool isEnabled,
        DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        SetValues(senderName, senderEmail, smtpHost, smtpPort, useSsl, username, passwordEncrypted, isEnabled);
        UpdatedAt = updatedAt;
    }

    public void Disable(DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        IsEnabled = false;
        UpdatedAt = updatedAt;
    }

    public void Enable(DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        IsEnabled = true;
        UpdatedAt = updatedAt;
    }

    private void SetValues(
        string senderName,
        string senderEmail,
        string smtpHost,
        int smtpPort,
        bool useSsl,
        string username,
        string passwordEncrypted,
        bool isEnabled)
    {
        SenderName = Require(senderName, nameof(senderName), 200);
        SenderEmail = RequireEmail(senderEmail, nameof(senderEmail), 320);
        SmtpHost = Require(smtpHost, nameof(smtpHost), 300);
        SmtpPort = RequirePort(smtpPort);
        UseSsl = useSsl;
        Username = Require(username, nameof(username), 320);
        PasswordEncrypted = Require(passwordEncrypted, nameof(passwordEncrypted), int.MaxValue);
        IsEnabled = isEnabled;
    }

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }

    private static string RequireEmail(string value, string parameterName, int maxLength)
    {
        var normalized = Require(value, parameterName, maxLength);

        try
        {
            _ = new MailAddress(normalized);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"{parameterName} must be a valid email", parameterName, ex);
        }

        return normalized;
    }

    private static int RequirePort(int smtpPort)
    {
        if (smtpPort < 1 || smtpPort > 65535)
            throw new ArgumentException("SmtpPort must be between 1 and 65535", nameof(smtpPort));

        return smtpPort;
    }
}
