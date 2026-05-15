using System.Net;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;
using Identity.Domain.Enums;

namespace CrmSystem.ApiTests.Identity;

public sealed class AuthAndPermissionsTests : ApiTestBase
{
    public AuthAndPermissionsTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Protected_endpoint_without_token_returns_unauthorized()
    {
        var response = await Client.GetAsync("/api/clients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_admin_can_call_protected_endpoint()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var response = await client.GetAsync("/api/identity/me");

        await response.AssertSuccessAsync();
    }

    [Fact]
    public async Task User_without_required_permission_receives_forbidden()
    {
        using var limited = await Auth.CreateLimitedUserClientAsync("Clients", PermissionAction.Read);

        var response = await limited.Client.GetAsync("/api/clients");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
