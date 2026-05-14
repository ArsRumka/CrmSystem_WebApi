using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Entities;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
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
        ("Email", "Email"),
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
        var moduleRoleRepository = scope.ServiceProvider.GetRequiredService<IModuleRoleRepository>();
        var systemAdminRepository = scope.ServiceProvider.GetRequiredService<ISystemAdminRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var systemAdminOptions = scope.ServiceProvider.GetRequiredService<IOptions<SystemAdminOptions>>().Value;
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Module? emailModule = null;
        foreach (var module in SystemModules)
        {
            var existingModule = await moduleRepository.GetByCodeAsync(module.Code, cancellationToken);
            if (existingModule is null)
            {
                existingModule = new Module(Guid.NewGuid(), module.Code, module.Name);
                await moduleRepository.AddAsync(existingModule, cancellationToken);
            }

            if (module.Code == "Email")
            {
                emailModule = existingModule;
            }
        }

        if (emailModule is not null)
        {
            var adminRoles = await dbContext.Set<Role>()
                .Where(x => x.Name == "Admin")
                .ToListAsync(cancellationToken);

            if (adminRoles.Count > 0)
            {
                var adminRoleIds = adminRoles.Select(x => x.Id).ToList();
                var roleIdsWithEmailPermission = await dbContext.Set<ModuleRole>()
                    .Where(x => x.ModuleId == emailModule.Id && adminRoleIds.Contains(x.RoleId))
                    .Select(x => x.RoleId)
                    .ToListAsync(cancellationToken);

                var existingRoleIds = roleIdsWithEmailPermission.ToHashSet();
                var missingEmailPermissions = adminRoles
                    .Where(x => !existingRoleIds.Contains(x.Id))
                    .Select(role => new ModuleRole(
                        Guid.NewGuid(),
                        role.OrganizationId,
                        role.Id,
                        emailModule.Id,
                        canRead: true,
                        canCreate: true,
                        canUpdate: true,
                        canDelete: true))
                    .ToList();

                if (missingEmailPermissions.Count > 0)
                {
                    await moduleRoleRepository.AddRangeAsync(missingEmailPermissions, cancellationToken);
                }
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
