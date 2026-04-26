using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clients.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddClientsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
