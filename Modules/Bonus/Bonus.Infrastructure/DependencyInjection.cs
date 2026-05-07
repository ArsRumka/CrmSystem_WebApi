using Bonus.Application.Abstractions.Lookups;
using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Abstractions.Services;
using Bonus.Infrastructure.Configurations;
using Bonus.Infrastructure.Repositories;
using Bonus.Infrastructure.Services;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Bonus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBonusInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(BonusSettingsConfiguration).Assembly));

        services.AddScoped<IBonusSettingsRepository, BonusSettingsRepository>();
        services.AddScoped<IBonusAccountRepository, BonusAccountRepository>();
        services.AddScoped<IBonusTransactionRepository, BonusTransactionRepository>();
        services.AddScoped<IBonusClientLookupService, BonusClientLookupService>();
        services.AddScoped<IBonusDealDiscountService, BonusDealDiscountService>();
        services.AddScoped<IBonusDealCompletionService, BonusDealCompletionService>();
        services.AddScoped<IBonusDealReturnService, BonusDealReturnService>();
        services.AddScoped<ICatalogBonusRuleResolver, CatalogBonusRuleResolver>();

        return services;
    }
}
