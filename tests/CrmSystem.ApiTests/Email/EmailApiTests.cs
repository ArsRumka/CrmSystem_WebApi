using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Email;

public sealed class EmailApiTests : ApiTestBase
{
    public EmailApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Manual_campaign_uses_fake_smtp_and_respects_recipient_skip_rules()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var settingsResponse = await client.PutAsJsonAsync("/api/email/settings", TestData.EmailSettings());
        await settingsResponse.AssertSuccessAsync();

        var getSettingsResponse = await client.GetAsync("/api/email/settings");
        await getSettingsResponse.AssertSuccessAsync();
        var settingsBody = (await getSettingsResponse.Content.ReadAsStringAsync()).ToLowerInvariant();
        settingsBody.Should().NotContain("smtppassword");
        settingsBody.Should().NotContain("passwordencrypted");

        var templateId = await CreateTemplateAsync(client);

        var normalClientId = await CreateClientAsync(client, email: "normal@example.test");
        var noEmailClientId = await CreatePhoneOnlyClientAsync(client);
        var marketingDisabledClientId = await CreateClientAsync(
            client,
            email: "disabled@example.test",
            allowMarketingEmails: false);

        var campaignResponse = await client.PostAsJsonAsync("/api/email/campaigns/manual", new
        {
            name = "Manual API test campaign",
            templateId,
            clientIds = new[] { normalClientId, noEmailClientId, marketingDisabledClientId }
        });
        await campaignResponse.AssertSuccessAsync();

        Guid campaignId;
        using (var campaignJson = await campaignResponse.ReadJsonDocumentAsync())
        {
            campaignId = campaignJson.RootElement.GetGuid("id");
            campaignJson.RootElement.GetInt32("totalRecipients").Should().Be(3);
        }

        var sendResponse = await client.PostAsync($"/api/email/campaigns/{campaignId}/send", content: null);
        await sendResponse.AssertSuccessAsync();

        using (var sentCampaignJson = await sendResponse.ReadJsonDocumentAsync())
        {
            sentCampaignJson.RootElement.GetInt32("sentCount").Should().Be(1);
            sentCampaignJson.RootElement.GetInt32("skippedCount").Should().Be(2);

            var recipients = sentCampaignJson.RootElement.GetProperty("recipients").EnumerateArray().ToList();
            recipients.Should().Contain(recipient =>
                recipient.GetGuid("clientId") == normalClientId &&
                recipient.GetInt32("status") == 2);
            recipients.Should().Contain(recipient =>
                recipient.GetGuid("clientId") == noEmailClientId &&
                recipient.GetInt32("status") == 4);
            recipients.Should().Contain(recipient =>
                recipient.GetGuid("clientId") == marketingDisabledClientId &&
                recipient.GetInt32("status") == 6);
        }

        Factory.OrganizationSmtpEmailSender.SentMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task Manual_automation_run_creates_campaign_for_inactive_client()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var settingsResponse = await client.PutAsJsonAsync("/api/email/settings", TestData.EmailSettings());
        await settingsResponse.AssertSuccessAsync();
        var templateId = await CreateTemplateAsync(client);

        Factory.Clock.SetUtcNow(DateTime.UtcNow.AddDays(-30));
        await CreateCompletedProductDealAsync(client, admin, quantity: 1m, productPrice: 100m);
        Factory.Clock.SetUtcNow(DateTime.UtcNow);

        var ruleResponse = await client.PutAsJsonAsync("/api/email/automation", new
        {
            isEnabled = true,
            templateId = (Guid?)templateId,
            inactivityDays = 10,
            repeatAfterDays = 1
        });
        await ruleResponse.AssertSuccessAsync();

        var runResponse = await client.PostAsync("/api/email/automation/run", content: null);
        await runResponse.AssertSuccessAsync();

        using var runJson = await runResponse.ReadJsonDocumentAsync();
        runJson.RootElement.GetBool("campaignCreated").Should().BeTrue();
        runJson.RootElement.GetInt32("candidateCount").Should().Be(1);
        runJson.RootElement.GetInt32("sentCount").Should().Be(1);

        Factory.OrganizationSmtpEmailSender.SentMessages.Should().ContainSingle();
    }

    private async Task<Guid> CreateTemplateAsync(HttpClient client)
    {
        var templateResponse = await client.PostAsJsonAsync(
            "/api/email/templates",
            TestData.EmailTemplate());
        await templateResponse.AssertSuccessAsync();

        using var templateJson = await templateResponse.ReadJsonDocumentAsync();
        return templateJson.RootElement.GetGuid("id");
    }

    private static async Task<Guid> CreatePhoneOnlyClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/clients", new
        {
            firstName = "Phone",
            lastName = "Only",
            middleName = (string?)null,
            email = (string?)null,
            phone = "+375291111111",
            status = 2,
            source = 2,
            allowMarketingEmails = true,
            notes = (string?)null
        });
        await response.AssertSuccessAsync();

        using var json = await response.ReadJsonDocumentAsync();
        return json.RootElement.GetGuid("id");
    }
}
