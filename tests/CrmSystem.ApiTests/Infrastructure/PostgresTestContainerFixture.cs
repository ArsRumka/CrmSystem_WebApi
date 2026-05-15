using Testcontainers.PostgreSql;

namespace CrmSystem.ApiTests.Infrastructure;

public sealed class PostgresTestContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("crm_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public CrmWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        Factory = new CrmWebApplicationFactory(_container.GetConnectionString());

        using var client = Factory.CreateClient(CrmWebApplicationFactory.DefaultClientOptions);
        await Factory.InitializeRespawnerAsync();
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await _container.DisposeAsync();
    }
}
