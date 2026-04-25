namespace Identity.Infrastructure.Security;

public sealed class SystemAdminOptions
{
    public string Name { get; init; } = "System Administrator";
    public string Email { get; init; } = "admin@crm.local";
    public string Password { get; init; } = "Admin123!";
}
