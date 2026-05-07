using Deals.Application.Abstractions.Services;
using Deals.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Deals.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDealsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<DealCalculationService>();
        services.AddScoped<DealReturnCalculationService>();
        services.AddScoped<IDealStageInitializer, DealStageInitializer>();

        return services;
    }
}
