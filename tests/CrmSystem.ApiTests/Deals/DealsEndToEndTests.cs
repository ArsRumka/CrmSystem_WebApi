using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Deals;

public sealed class DealsEndToEndTests : ApiTestBase
{
    public DealsEndToEndTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Completing_deal_deducts_stock_writes_bonus_transactions_and_audit_log()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var scenario = await CreateCompletedProductDealAsync(
            client,
            admin,
            quantity: 2m,
            productPrice: 100m,
            bonusPointsUsed: 20m);

        scenario.FinalAmount.Should().Be(180m);
        (await GetStockQuantityAsync(client, scenario.StorageId, scenario.ProductId)).Should().Be(8m);

        var movementsResponse = await client.GetAsync($"/api/warehouse/movements?dealId={scenario.DealId}");
        await movementsResponse.AssertSuccessAsync();
        using (var movementsJson = await movementsResponse.ReadJsonDocumentAsync())
        {
            movementsJson.RootElement
                .EnumerateArray()
                .Should()
                .Contain(movement =>
                    movement.GetInt32("type") == 3 &&
                    movement.GetDecimal("quantity") == 2m);
        }

        var transactionsResponse = await client.GetAsync($"/api/bonus/transactions?dealId={scenario.DealId}");
        await transactionsResponse.AssertSuccessAsync();
        using (var transactionsJson = await transactionsResponse.ReadJsonDocumentAsync())
        {
            var types = transactionsJson.RootElement
                .EnumerateArray()
                .Select(transaction => transaction.GetInt32("type"))
                .ToList();

            types.Should().Contain(2);
            types.Should().Contain(1);
        }

        var dealResponse = await client.GetAsync($"/api/deals/{scenario.DealId}");
        await dealResponse.AssertSuccessAsync();
        using (var dealJson = await dealResponse.ReadJsonDocumentAsync())
        {
            dealJson.RootElement.GetDecimal("finalAmount").Should().Be(180m);
            dealJson.RootElement.GetDecimal("bonusPointsUsed").Should().Be(20m);
            dealJson.RootElement.GetDecimal("bonusDiscountAmount").Should().Be(20m);
        }

        var auditResponse = await client.GetAsync("/api/audit/logs?moduleCode=Deals&take=50");
        await auditResponse.AssertSuccessAsync();
        using var auditJson = await auditResponse.ReadJsonDocumentAsync();
        auditJson.RootElement
            .EnumerateArray()
            .Should()
            .Contain(log =>
                log.GetString("entityName") == "Deal" &&
                log.GetGuid("entityId") == scenario.DealId);
    }
}
