using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Catalog;

public sealed class CatalogApiTests : ApiTestBase
{
    public CatalogApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Category_product_and_service_can_be_created_and_listed()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var categoryId = await CreateCategoryAsync(client, "API Category");
        var productId = await CreateProductAsync(client, categoryId, price: 125m, name: "API Product");
        var serviceId = await CreateServiceAsync(client, categoryId, price: 75m, name: "API Service");

        var categoriesResponse = await client.GetAsync("/api/catalog/categories");
        var productsResponse = await client.GetAsync("/api/catalog/products");
        var servicesResponse = await client.GetAsync("/api/catalog/services");

        await categoriesResponse.AssertSuccessAsync();
        await productsResponse.AssertSuccessAsync();
        await servicesResponse.AssertSuccessAsync();

        using var categoriesJson = await categoriesResponse.ReadJsonDocumentAsync();
        using var productsJson = await productsResponse.ReadJsonDocumentAsync();
        using var servicesJson = await servicesResponse.ReadJsonDocumentAsync();

        categoriesJson.RootElement.FindByGuid("id", categoryId).Should().NotBeNull();
        productsJson.RootElement.FindByGuid("id", productId).Should().NotBeNull();
        servicesJson.RootElement.FindByGuid("id", serviceId).Should().NotBeNull();
    }
}
