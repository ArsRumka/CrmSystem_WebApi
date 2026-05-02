using Clients.Application.Abstractions.Repositories;
using Clients.Infrastructure.Configurations;
using Clients.Infrastructure.Repositories;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Clients.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddClientsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(ClientConfiguration).Assembly));

        services.AddScoped<IClientRepository, ClientRepository>();

        return services;
    }
}
