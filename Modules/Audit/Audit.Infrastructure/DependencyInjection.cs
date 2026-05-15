using Audit.Application.Abstractions.Repositories;
using Audit.Application.Abstractions.Services;
using Audit.Infrastructure.Configurations;
using Audit.Infrastructure.Repositories;
using Audit.Infrastructure.Services;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(AuditLogConfiguration).Assembly));

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}

