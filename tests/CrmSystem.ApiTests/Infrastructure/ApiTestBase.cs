using System.Net.Http.Json;

namespace CrmSystem.ApiTests.Infrastructure;

[Collection(ApiTestCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    protected ApiTestBase(PostgresTestContainerFixture fixture)
    {
        Factory = fixture.Factory;
        Auth = new AuthTestClient(Factory);
        Client = Factory.CreateClient(CrmWebApplicationFactory.DefaultClientOptions);
    }

    protected CrmWebApplicationFactory Factory { get; }

    protected AuthTestClient Auth { get; }

    protected HttpClient Client { get; }

    public virtual Task InitializeAsync()
    {
        return Factory.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<Guid> CreateClientAsync(
        HttpClient client,
        string? email = null,
        string? phone = null,
        bool allowMarketingEmails = true,
        string? suffix = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/clients",
            TestData.Client(email, phone, allowMarketingEmails, suffix));

        await response.AssertSuccessAsync();
        using var json = await response.ReadJsonDocumentAsync();
        return json.RootElement.GetGuid("id");
    }

    protected async Task<Guid> CreateCategoryAsync(HttpClient client, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/api/catalog/categories", TestData.Category(name));
        await response.AssertSuccessAsync();
        using var json = await response.ReadJsonDocumentAsync();
        return json.RootElement.GetGuid("id");
    }

    protected async Task<Guid> CreateProductAsync(
        HttpClient client,
        Guid? categoryId = null,
        decimal price = 100m,
        string? name = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/catalog/products",
            TestData.Product(categoryId, price, name));

        await response.AssertSuccessAsync();
        using var json = await response.ReadJsonDocumentAsync();
        return json.RootElement.GetGuid("id");
    }

    protected async Task<Guid> CreateServiceAsync(
        HttpClient client,
        Guid? categoryId = null,
        decimal price = 50m,
        string? name = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/catalog/services",
            TestData.Service(categoryId, price, name));

        await response.AssertSuccessAsync();
        using var json = await response.ReadJsonDocumentAsync();
        return json.RootElement.GetGuid("id");
    }

    protected async Task<Guid> CreateStorageAsync(HttpClient client, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/api/warehouse/storages", TestData.Storage(name));
        await response.AssertSuccessAsync();
        using var json = await response.ReadJsonDocumentAsync();
        return json.RootElement.GetGuid("id");
    }

    protected async Task ReceiptStockAsync(
        HttpClient client,
        Guid storageId,
        Guid productId,
        decimal quantity)
    {
        var response = await client.PostAsJsonAsync("/api/warehouse/stocks/receipt", new
        {
            storageId,
            productId,
            quantity,
            reason = "Test receipt"
        });

        await response.AssertSuccessAsync();
    }

    protected async Task<decimal> GetStockQuantityAsync(
        HttpClient client,
        Guid storageId,
        Guid productId)
    {
        var response = await client.GetAsync(
            $"/api/warehouse/stocks?storageId={storageId}&productId={productId}");

        await response.AssertSuccessAsync();
        using var json = await response.ReadJsonDocumentAsync();
        var stock = json.RootElement.EnumerateArray().Single();
        return stock.GetDecimal("quantity");
    }

    protected async Task<CompletedProductDealScenario> CreateCompletedProductDealAsync(
        HttpClient client,
        TestUser user,
        decimal quantity = 2m,
        decimal productPrice = 100m,
        decimal bonusPointsUsed = 0m)
    {
        var clientId = await CreateClientAsync(client);
        var productId = await CreateProductAsync(client, price: productPrice);
        var storageId = await CreateStorageAsync(client);

        await ReceiptStockAsync(client, storageId, productId, quantity + 8m);

        if (bonusPointsUsed > 0)
        {
            var settingsResponse = await client.PutAsJsonAsync(
                "/api/bonus/settings",
                TestData.BonusSettings());
            await settingsResponse.AssertSuccessAsync();

            var adjustmentResponse = await client.PostAsJsonAsync(
                $"/api/bonus/accounts/by-client/{clientId}/adjust",
                new
                {
                    pointsDelta = bonusPointsUsed + 30m,
                    reason = "Test bonus setup"
                });
            await adjustmentResponse.AssertSuccessAsync();
        }

        var dealResponse = await client.PostAsJsonAsync("/api/deals", new
        {
            clientId,
            responsibleUserId = user.UserId,
            bonusPointsUsed,
            notes = "API test deal",
            items = new[]
            {
                new
                {
                    itemType = 1,
                    itemId = productId,
                    storageId = (Guid?)storageId,
                    quantity,
                    manualDiscountType = (int?)null,
                    manualDiscountValue = (decimal?)null
                }
            }
        });
        await dealResponse.AssertSuccessAsync();

        Guid dealId;
        Guid dealItemId;
        decimal finalAmount;
        using (var dealJson = await dealResponse.ReadJsonDocumentAsync())
        {
            dealId = dealJson.RootElement.GetGuid("id");
            finalAmount = dealJson.RootElement.GetDecimal("finalAmount");
            dealItemId = dealJson.RootElement
                .GetProperty("items")
                .EnumerateArray()
                .Single()
                .GetGuid("id");
        }

        var stagesResponse = await client.GetAsync("/api/deals/stages");
        await stagesResponse.AssertSuccessAsync();

        Guid completedStageId;
        using (var stagesJson = await stagesResponse.ReadJsonDocumentAsync())
        {
            var completedStage = stagesJson.RootElement
                .EnumerateArray()
                .Single(stage => stage.GetString("name") == "Completed");
            completedStageId = completedStage.GetGuid("id");
        }

        var completeResponse = await client.PutAsJsonAsync(
            $"/api/deals/{dealId}/stage",
            new { stageId = completedStageId });
        await completeResponse.AssertSuccessAsync();

        return new CompletedProductDealScenario(
            clientId,
            productId,
            storageId,
            dealId,
            dealItemId,
            quantity,
            finalAmount);
    }

    protected sealed record CompletedProductDealScenario(
        Guid ClientId,
        Guid ProductId,
        Guid StorageId,
        Guid DealId,
        Guid DealItemId,
        decimal Quantity,
        decimal FinalAmount);
}
