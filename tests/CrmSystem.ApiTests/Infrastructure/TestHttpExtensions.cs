using System.Net;
using System.Text.Json;
using Xunit.Sdk;

namespace CrmSystem.ApiTests.Infrastructure;

public static class TestHttpExtensions
{
    public static async Task<HttpResponseMessage> AssertSuccessAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new XunitException(
                $"Expected success status code, got {(int)response.StatusCode} {response.StatusCode}.{Environment.NewLine}{body}");
        }

        return response;
    }

    public static async Task<HttpResponseMessage> AssertStatusCodeAsync(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode != expectedStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new XunitException(
                $"Expected {(int)expectedStatusCode} {expectedStatusCode}, got {(int)response.StatusCode} {response.StatusCode}.{Environment.NewLine}{body}");
        }

        return response;
    }

    public static async Task<JsonDocument> ReadJsonDocumentAsync(this HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
