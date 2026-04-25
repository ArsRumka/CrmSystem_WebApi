using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Infrastructure.Permissions;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<IModuleRoleRepository, ModuleRoleRepository>();
        services.AddScoped<ISystemAdminRepository, SystemAdminRepository>();
        services.AddScoped<IOrganizationRequestRepository, OrganizationRequestRepository>();
        services.AddScoped<IActivationKeyRepository, ActivationKeyRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenHasher, Sha256TokenHasher>();
        services.AddScoped<IActivationKeyGenerator, ActivationKeyGenerator>();
        services.AddScoped<ITokenGenerator, SecureTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddHostedService<IdentitySeedHostedService>();

        return services;
    }
}
