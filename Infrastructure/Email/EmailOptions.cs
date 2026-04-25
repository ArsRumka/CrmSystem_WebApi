namespace Infrastructure.Email;

public sealed class EmailOptions
{
    public bool UseConsole { get; init; } = false;

    public string From { get; init; } = "noreply@crm.local";
    public string DisplayName { get; init; } = "CRM System";

    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
