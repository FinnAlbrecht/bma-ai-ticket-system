using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketSystem.Infrastructure.Ai;
using TicketSystem.Infrastructure.Ai.Clients;

namespace TicketSystem.Tests;

public class OpenRouterMessageClientTests
{
    [Fact]
    public async Task CreateMessageAsync_SendsChatCompletionRequestAndReturnsModelText()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"content":"{\"isItRelated\":true}"}}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var httpClient = new HttpClient(handler);
        var client = new OpenRouterMessageClient(
            httpClient,
            Options.Create(new OpenRouterOptions
            {
                ApiKey = "test-key",
                Model = "nvidia/nemotron-3-ultra-550b-a55b",
                Endpoint = "https://openrouter.example/v1/chat/completions",
                MaxTokens = 321
            }));

        var result = await client.CreateMessageAsync("system prompt", "ticket prompt", CancellationToken.None);

        Assert.Equal("{\"isItRelated\":true}", result);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("test-key", capturedRequest.Headers.Authorization.Parameter);
        Assert.Equal("https://openrouter.example/v1/chat/completions", capturedRequest.RequestUri!.ToString());
        using var body = JsonDocument.Parse(capturedBody!);
        Assert.Equal("nvidia/nemotron-3-ultra-550b-a55b", body.RootElement.GetProperty("model").GetString());
        Assert.Equal(321, body.RootElement.GetProperty("max_tokens").GetInt32());
        var messages = body.RootElement.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("system prompt", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("ticket prompt", messages[1].GetProperty("content").GetString());
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => sendAsync(request);
    }
}
