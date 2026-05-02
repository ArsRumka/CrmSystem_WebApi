using Catalog.Application.Abstractions.Repositories;
using Catalog.Infrastructure.Configurations;
using Catalog.Infrastructure.Repositories;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(CategoryConfiguration).Assembly));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();

        return services;
    }
}
