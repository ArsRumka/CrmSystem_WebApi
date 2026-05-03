using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Abstractions.Services;
using Warehouse.Infrastructure.Configurations;
using Warehouse.Infrastructure.Repositories;
using Warehouse.Infrastructure.Services;

namespace Warehouse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWarehouseInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(StorageConfiguration).Assembly));

        services.AddScoped<IStorageRepository, StorageRepository>();
        services.AddScoped<IProductStockRepository, ProductStockRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IWarehouseProductLookupService, WarehouseProductLookupService>();
        services.AddScoped<IWarehouseDealCompletionService, WarehouseDealCompletionService>();

        return services;
    }
}

