namespace Identity.Application.Common;

internal static class IdentityApplicationConstants
{
    public const string AdminRoleName = "Admin";
    public const string UsersModuleCode = "Users";
    public const string RolesModuleCode = "Roles";

    public const int EmailConfirmationTokenLifetimeHours = 24;
    public const int PasswordResetTokenLifetimeHours = 1;
}
