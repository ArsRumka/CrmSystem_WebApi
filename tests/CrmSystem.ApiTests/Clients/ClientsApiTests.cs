using System.Net;
using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Clients;

public sealed class ClientsApiTests : ApiTestBase
{
    public ClientsApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Client_lifecycle_uses_real_http_endpoints()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var createResponse = await client.PostAsJsonAsync(
            "/api/clients",
            TestData.Client(email: "client-lifecycle@example.test", allowMarketingEmails: true));
        await createResponse.AssertSuccessAsync();

        Guid clientId;
        using (var createdJson = await createResponse.ReadJsonDocumentAsync())
        {
            createdJson.RootElement.GetString("email").Should().Be("client-lifecycle@example.test");
            createdJson.RootElement.GetBool("allowMarketingEmails").Should().BeTrue();
            clientId = createdJson.RootElement.GetGuid("id");
        }

        var updateResponse = await client.PutAsJsonAsync($"/api/clients/{clientId}", new
        {
            firstName = "Updated",
            lastName = "Client",
            middleName = (string?)null,
            email = "updated-client@example.test",
            phone = "+375291234567",
            status = 2,
            source = 4,
            allowMarketingEmails = false,
            notes = "Updated by API test"
        });
        await updateResponse.AssertSuccessAsync();

        using (var updatedJson = await updateResponse.ReadJsonDocumentAsync())
        {
            updatedJson.RootElement.GetString("firstName").Should().Be("Updated");
            updatedJson.RootElement.GetString("email").Should().Be("updated-client@example.test");
            updatedJson.RootElement.GetBool("allowMarketingEmails").Should().BeFalse();
        }

        var deactivateResponse = await client.DeleteAsync($"/api/clients/{clientId}");
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/clients/{clientId}");
        await getResponse.AssertSuccessAsync();

        using var deactivatedJson = await getResponse.ReadJsonDocumentAsync();
        deactivatedJson.RootElement.GetBool("isActive").Should().BeFalse();
    }

    [Fact]
    public async Task Creating_client_without_email_or_phone_returns_validation_error()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var response = await client.PostAsJsonAsync("/api/clients", new
        {
            firstName = "No",
            lastName = "Contact",
            middleName = (string?)null,
            email = (string?)null,
            phone = (string?)null,
            status = 2,
            source = 3,
            allowMarketingEmails = true,
            notes = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
