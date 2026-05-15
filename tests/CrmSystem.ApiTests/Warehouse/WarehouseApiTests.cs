using System.Net;
using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Warehouse;

public sealed class WarehouseApiTests : ApiTestBase
{
    public WarehouseApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Receipt_writeoff_and_excessive_writeoff_use_real_stock_endpoints()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var productId = await CreateProductAsync(client);
        var storageId = await CreateStorageAsync(client);

        await ReceiptStockAsync(client, storageId, productId, 10m);
        (await GetStockQuantityAsync(client, storageId, productId)).Should().Be(10m);

        var writeOffResponse = await client.PostAsJsonAsync("/api/warehouse/stocks/write-off", new
        {
            storageId,
            productId,
            quantity = 4m,
            reason = "Test write-off"
        });
        await writeOffResponse.AssertSuccessAsync();

        using (var writeOffJson = await writeOffResponse.ReadJsonDocumentAsync())
        {
            writeOffJson.RootElement.GetDecimal("quantity").Should().Be(6m);
        }

        var excessiveResponse = await client.PostAsJsonAsync("/api/warehouse/stocks/write-off", new
        {
            storageId,
            productId,
            quantity = 7m,
            reason = "Excessive test write-off"
        });

        excessiveResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
