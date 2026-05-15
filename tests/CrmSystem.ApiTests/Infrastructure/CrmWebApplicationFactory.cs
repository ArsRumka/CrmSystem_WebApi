using BuildingBlocks.Application.Abstractions.Email;
using BuildingBlocks.Application.Abstractions.Time;
using Email.Application.Abstractions.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace CrmSystem.ApiTests.Infrastructure;

public sealed class CrmWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _resetLock = new(1, 1);
    private Respawner? _respawner;

    public CrmWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static WebApplicationFactoryClientOptions DefaultClientOptions { get; } = new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    };

    public FakeOrganizationSmtpEmailSender OrganizationSmtpEmailSender =>
        Services.GetRequiredService<FakeOrganizationSmtpEmailSender>();

    public FakeEmailSender EmailSender =>
        Services.GetRequiredService<FakeEmailSender>();

    public TestClock Clock =>
        Services.GetRequiredService<TestClock>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["EmailAutomation:IsEnabled"] = "false",
                ["EmailAutomation:IntervalHours"] = "24",
                ["Email:UseConsole"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_connectionString));

            services.RemoveEmailAutomationHostedService();
            services.InsertDatabaseMigrationHostedServiceBeforeIdentitySeed();

            services.RemoveAll<IOrganizationSmtpEmailSender>();
            services.AddSingleton<FakeOrganizationSmtpEmailSender>();
            services.AddSingleton<IOrganizationSmtpEmailSender>(provider =>
                provider.GetRequiredService<FakeOrganizationSmtpEmailSender>());

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(provider =>
                provider.GetRequiredService<FakeEmailSender>());

            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<TestClock>();
            services.AddSingleton<IDateTimeProvider>(provider =>
                provider.GetRequiredService<TestClock>());
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner was not initialized.");
        }

        await _resetLock.WaitAsync();
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await _respawner.ResetAsync(connection);
            ResetTestDoubles();
        }
        finally
        {
            _resetLock.Release();
        }
    }

    public async Task<TResult> ExecuteScopeAsync<TResult>(
        Func<IServiceProvider, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    public async Task InitializeRespawnerAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                new Table("__EFMigrationsHistory"),
                new Table("Modules"),
                new Table("SystemAdmins")
            ]
        });
    }

    private void ResetTestDoubles()
    {
        OrganizationSmtpEmailSender.Clear();
        EmailSender.Clear();
        Clock.Reset();
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveEmailAutomationHostedService(this IServiceCollection services)
    {
        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];

            if (descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType?.FullName ==
                "Email.Infrastructure.Services.EmailAutomationHostedService")
            {
                services.RemoveAt(index);
            }
        }
    }

    public static void InsertDatabaseMigrationHostedServiceBeforeIdentitySeed(this IServiceCollection services)
    {
        var descriptor = ServiceDescriptor.Singleton<IHostedService, DatabaseMigrationHostedService>();
        var identitySeedIndex = services
            .Select((service, index) => (service, index))
            .FirstOrDefault(x =>
                x.service.ServiceType == typeof(IHostedService) &&
                x.service.ImplementationType?.FullName ==
                "Identity.Infrastructure.Seed.IdentitySeedHostedService")
            .index;

        if (identitySeedIndex > 0)
        {
            services.Insert(identitySeedIndex, descriptor);
            return;
        }

        services.Add(descriptor);
    }
}

internal sealed class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseMigrationHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
