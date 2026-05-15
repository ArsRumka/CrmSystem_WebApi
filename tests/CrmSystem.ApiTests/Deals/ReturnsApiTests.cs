using System.Net;
using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Deals;

public sealed class ReturnsApiTests : ApiTestBase
{
    public ReturnsApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Completing_return_restores_stock_and_creates_bonus_corrections()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);
        var scenario = await CreateCompletedProductDealAsync(
            client,
            admin,
            quantity: 2m,
            productPrice: 100m,
            bonusPointsUsed: 20m);

        var stockAfterDeal = await GetStockQuantityAsync(client, scenario.StorageId, scenario.ProductId);

        var createReturnResponse = await client.PostAsJsonAsync($"/api/deals/{scenario.DealId}/returns", new
        {
            reason = "Customer returned one item",
            items = new[]
            {
                new
                {
                    dealItemId = scenario.DealItemId,
                    quantity = 1m,
                    storageId = (Guid?)scenario.StorageId
                }
            }
        });
        await createReturnResponse.AssertSuccessAsync();

        Guid returnId;
        using (var returnJson = await createReturnResponse.ReadJsonDocumentAsync())
        {
            returnJson.RootElement.GetInt32("status").Should().Be(1);
            returnId = returnJson.RootElement.GetGuid("id");
        }

        (await GetStockQuantityAsync(client, scenario.StorageId, scenario.ProductId))
            .Should()
            .Be(stockAfterDeal);

        var completeResponse = await client.PostAsync($"/api/deals/returns/{returnId}/complete", content: null);
        await completeResponse.AssertSuccessAsync();

        using (var completedReturnJson = await completeResponse.ReadJsonDocumentAsync())
        {
            completedReturnJson.RootElement.GetInt32("status").Should().Be(2);
            completedReturnJson.RootElement.GetDecimal("bonusPointsReturned").Should().BeGreaterThan(0);
            completedReturnJson.RootElement.GetDecimal("bonusAccrualReversed").Should().BeGreaterThan(0);
        }

        (await GetStockQuantityAsync(client, scenario.StorageId, scenario.ProductId))
            .Should()
            .Be(stockAfterDeal + 1m);

        var movementsResponse = await client.GetAsync($"/api/warehouse/movements?dealId={scenario.DealId}");
        await movementsResponse.AssertSuccessAsync();
        using (var movementsJson = await movementsResponse.ReadJsonDocumentAsync())
        {
            movementsJson.RootElement
                .EnumerateArray()
                .Should()
                .Contain(movement => movement.GetInt32("type") == 4);
        }

        var transactionsResponse = await client.GetAsync($"/api/bonus/transactions?dealId={scenario.DealId}");
        await transactionsResponse.AssertSuccessAsync();
        using (var transactionsJson = await transactionsResponse.ReadJsonDocumentAsync())
        {
            var types = transactionsJson.RootElement
                .EnumerateArray()
                .Select(transaction => transaction.GetInt32("type"))
                .ToList();

            types.Should().Contain(3);
            types.Should().Contain(5);
        }

        var overReturnResponse = await client.PostAsJsonAsync($"/api/deals/{scenario.DealId}/returns", new
        {
            reason = "Over-return",
            items = new[]
            {
                new
                {
                    dealItemId = scenario.DealItemId,
                    quantity = 2m,
                    storageId = (Guid?)scenario.StorageId
                }
            }
        });

        overReturnResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
