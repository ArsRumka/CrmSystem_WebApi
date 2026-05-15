namespace CrmSystem.ApiTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiTestCollection : ICollectionFixture<PostgresTestContainerFixture>
{
    public const string Name = "API integration tests";
}
