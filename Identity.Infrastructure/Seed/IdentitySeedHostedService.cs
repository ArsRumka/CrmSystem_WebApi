using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Entities;
using Identity.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Seed;

public sealed class IdentitySeedHostedService : IHostedService
{
    private static readonly (string Code, string Name)[] SystemModules =
    [
        ("Users", "Users"),
        ("Roles", "Roles"),
        ("Clients", "Clients"),
        ("Deals", "Deals"),
        ("Catalog", "Catalog"),
        ("Bonus", "Bonus"),
        ("Warehouse", "Warehouse"),
        ("Chat", "Chat"),
        ("Audit", "Audit"),
        ("Settings", "Settings")
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdentitySeedHostedService> _logger;

    public IdentitySeedHostedService(IServiceScopeFactory scopeFactory, ILogger<IdentitySeedHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var moduleRepository = scope.ServiceProvider.GetRequiredService<IModuleRepository>();
        var systemAdminRepository = scope.ServiceProvider.GetRequiredService<ISystemAdminRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var systemAdminOptions = scope.ServiceProvider.GetRequiredService<IOptions<SystemAdminOptions>>().Value;

        foreach (var module in SystemModules)
        {
            if (!await moduleRepository.ExistsByCodeAsync(module.Code, cancellationToken))
            {
                await moduleRepository.AddAsync(new Module(Guid.NewGuid(), module.Code, module.Name), cancellationToken);
            }
        }

        if (!await systemAdminRepository.ExistsByEmailAsync(systemAdminOptions.Email, cancellationToken))
        {
            var systemAdmin = new SystemAdmin(
                Guid.NewGuid(),
                systemAdminOptions.Name,
                systemAdminOptions.Email,
                passwordHasher.Hash(systemAdminOptions.Password),
                dateTimeProvider.UtcNow);

            await systemAdminRepository.AddAsync(systemAdmin, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Identity seed completed");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
