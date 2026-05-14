using Email.Domain.Entities;

namespace Email.Application.Contracts;

public sealed record EmailSettingsResponse(
    bool IsConfigured,
    Guid? Id,
    Guid? OrganizationId,
    string? SenderName,
    string? SenderEmail,
    string? SmtpHost,
    int? SmtpPort,
    bool UseSsl,
    string? Username,
    bool HasPassword,
    bool IsEnabled,
    DateTime? CreatedAt,
    DateTime? UpdatedAt)
{
    public static EmailSettingsResponse NotConfigured()
    {
        return new EmailSettingsResponse(
            IsConfigured: false,
            Id: null,
            OrganizationId: null,
            SenderName: null,
            SenderEmail: null,
            SmtpHost: null,
            SmtpPort: null,
            UseSsl: true,
            Username: null,
            HasPassword: false,
            IsEnabled: false,
            CreatedAt: null,
            UpdatedAt: null);
    }
}

internal static class EmailSettingsResponseMapper
{
    public static EmailSettingsResponse ToResponse(this EmailSettings settings)
    {
        return new EmailSettingsResponse(
            IsConfigured: true,
            settings.Id,
            settings.OrganizationId,
            settings.SenderName,
            settings.SenderEmail,
            settings.SmtpHost,
            settings.SmtpPort,
            settings.UseSsl,
            settings.Username,
            HasPassword: !string.IsNullOrWhiteSpace(settings.PasswordEncrypted),
            settings.IsEnabled,
            settings.CreatedAt,
            settings.UpdatedAt);
    }
}
