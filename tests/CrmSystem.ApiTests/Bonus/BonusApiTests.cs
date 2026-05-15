using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Bonus;

public sealed class BonusApiTests : ApiTestBase
{
    public BonusApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Bonus_settings_and_manual_adjustment_can_be_used_via_api()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);
        var clientId = await CreateClientAsync(client);

        var settingsResponse = await client.PutAsJsonAsync("/api/bonus/settings", TestData.BonusSettings());
        await settingsResponse.AssertSuccessAsync();

        var adjustResponse = await client.PostAsJsonAsync(
            $"/api/bonus/accounts/by-client/{clientId}/adjust",
            new
            {
                pointsDelta = 25m,
                reason = "Manual test adjustment"
            });
        await adjustResponse.AssertSuccessAsync();

        using (var accountJson = await adjustResponse.ReadJsonDocumentAsync())
        {
            accountJson.RootElement.GetDecimal("balance").Should().Be(25m);
        }

        var transactionsResponse = await client.GetAsync($"/api/bonus/transactions?clientId={clientId}");
        await transactionsResponse.AssertSuccessAsync();
        using var transactionsJson = await transactionsResponse.ReadJsonDocumentAsync();
        transactionsJson.RootElement
            .EnumerateArray()
            .Should()
            .Contain(transaction => transaction.GetInt32("type") == 4);
    }
}
