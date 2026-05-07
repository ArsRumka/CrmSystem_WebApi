using Deals.Application.Abstractions.Lookups;
using Deals.Application.Abstractions.Repositories;
using Deals.Infrastructure.Configurations;
using Deals.Infrastructure.Lookups;
using Deals.Infrastructure.Repositories;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Deals.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDealsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(DealConfiguration).Assembly));

        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IDealReturnRepository, DealReturnRepository>();
        services.AddScoped<IDealStageRepository, DealStageRepository>();
        services.AddScoped<IDealStageHistoryRepository, DealStageHistoryRepository>();
        services.AddScoped<IClientLookupService, ClientLookupService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<ICatalogLookupService, CatalogLookupService>();

        return services;
    }
}
