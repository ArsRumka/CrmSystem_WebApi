using System.Net;
using System.Net.Http.Json;
using CrmSystem.ApiTests.Infrastructure;
using FluentAssertions;

namespace CrmSystem.ApiTests.Chat;

public sealed class ChatApiTests : ApiTestBase
{
    public ChatApiTests(PostgresTestContainerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Group_conversation_can_send_and_read_messages_via_rest_api()
    {
        var admin = await Auth.CreateOrganizationWithAdminAsync();
        var participant = await Auth.CreateUserAsync(admin);
        using var client = await Auth.CreateAuthenticatedClientAsync(admin);

        var conversationResponse = await client.PostAsJsonAsync("/api/chat/conversations", new
        {
            type = 2,
            title = "API test group",
            participantUserIds = new[] { participant.UserId },
            clientId = (Guid?)null,
            dealId = (Guid?)null
        });
        await conversationResponse.AssertSuccessAsync();

        Guid conversationId;
        using (var conversationJson = await conversationResponse.ReadJsonDocumentAsync())
        {
            conversationId = conversationJson.RootElement.GetGuid("id");
            conversationJson.RootElement.GetInt32("type").Should().Be(2);
            conversationJson.RootElement.GetProperty("participants").GetArrayLength().Should().Be(2);
        }

        var sendResponse = await client.PostAsJsonAsync(
            $"/api/chat/conversations/{conversationId}/messages",
            new { text = "Hello from REST API" });
        await sendResponse.AssertSuccessAsync();

        var messagesResponse = await client.GetAsync($"/api/chat/conversations/{conversationId}/messages");
        await messagesResponse.AssertSuccessAsync();

        using var messagesJson = await messagesResponse.ReadJsonDocumentAsync();
        messagesJson.RootElement
            .EnumerateArray()
            .Should()
            .Contain(message => message.GetString("text") == "Hello from REST API");
    }

    [Fact]
    public async Task Contact_request_approval_creates_inter_org_conversation_visible_only_to_participants()
    {
        var requester = await Auth.CreateOrganizationWithAdminAsync("requester");
        var target = await Auth.SeedSecondOrganizationAsync("target");
        var outsider = await Auth.CreateUserAsync(requester);

        using var requesterClient = await Auth.CreateAuthenticatedClientAsync(requester);
        using var targetClient = await Auth.CreateAuthenticatedClientAsync(target);
        using var outsiderClient = await Auth.CreateAuthenticatedClientAsync(outsider);

        var requestResponse = await requesterClient.PostAsJsonAsync("/api/chat/contact-requests", new
        {
            targetOrganizationEmail = target.OrganizationEmail,
            message = "Let's connect"
        });
        await requestResponse.AssertSuccessAsync();

        Guid requestId;
        using (var requestJson = await requestResponse.ReadJsonDocumentAsync())
        {
            requestId = requestJson.RootElement.GetGuid("id");
        }

        var approveResponse = await targetClient.PostAsync(
            $"/api/chat/contact-requests/{requestId}/approve",
            content: null);
        await approveResponse.AssertSuccessAsync();

        Guid conversationId;
        using (var conversationJson = await approveResponse.ReadJsonDocumentAsync())
        {
            conversationJson.RootElement.GetInt32("type").Should().Be(5);
            conversationJson.RootElement.GetProperty("organizations").GetArrayLength().Should().Be(2);
            conversationId = conversationJson.RootElement.GetGuid("id");
        }

        var outsiderResponse = await outsiderClient.GetAsync($"/api/chat/conversations/{conversationId}");

        outsiderResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
