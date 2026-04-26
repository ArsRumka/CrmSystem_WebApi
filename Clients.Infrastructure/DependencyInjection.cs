using Clients.Application.Abstractions.Repositories;
using Clients.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Clients.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddClientsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();

        return services;
    }
}
