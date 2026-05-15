using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Audit;

public sealed class AuditApiTests : ApiTestBase
{
    public AuditApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Audited_client_action_is_queryable()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var clientId = await CreateClientAsync(client);

        var auditResponse = await client.GetAsync("/api/audit/logs?moduleCode=Clients&take=20");
        await auditResponse.AssertSuccessAsync();

        using var auditJson = await auditResponse.ReadJsonDocumentAsync();
        auditJson.RootElement
            .EnumerateArray()
            .Should()
            .Contain(log =>
                log.GetString("moduleCode") == "Clients" &&
                log.GetString("entityName") == "Client" &&
                log.GetGuid("entityId") == clientId);
    }

    [Fact]
    public async Task Email_settings_audit_payload_does_not_expose_sensitive_values()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var updateResponse = await client.PutAsJsonAsync("/api/email/settings", TestData.EmailSettings());
        await updateResponse.AssertSuccessAsync();

        var auditResponse = await client.GetAsync(
            "/api/audit/logs?moduleCode=Email&entityName=EmailSettings&take=20");
        await auditResponse.AssertSuccessAsync();

        using var auditJson = await auditResponse.ReadJsonDocumentAsync();
        var emailSettingsLog = auditJson.RootElement
            .EnumerateArray()
            .Single(log => log.GetString("entityName") == "EmailSettings");

        var payload = string.Join(
            " ",
            emailSettingsLog.GetProperty("oldValuesJson").GetString(),
            emailSettingsLog.GetProperty("newValuesJson").GetString())
            .ToLowerInvariant();

        payload.Should().NotContain("passwordencrypted");
        payload.Should().NotContain("smtppassword");
        payload.Should().NotContain("password");
        payload.Should().NotContain("token");
    }
}
