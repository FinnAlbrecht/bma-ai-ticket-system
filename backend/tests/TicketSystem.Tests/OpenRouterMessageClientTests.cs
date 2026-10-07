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
    public async Task CreateMessageAsync_PreservesUpstreamHttpStatus()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        using var httpClient = new HttpClient(handler);
        var client = new OpenRouterMessageClient(
            httpClient,
            Options.Create(new OpenRouterOptions { ApiKey = "test-key" }));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.CreateMessageAsync("system", "user", CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task CreateMessageAsync_ExplainsSuccessfulResponseWithoutMessageContent()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"reasoning":"internal"},"finish_reason":"length"}]}""")
            }));
        using var httpClient = new HttpClient(handler);
        var client = new OpenRouterMessageClient(
            httpClient,
            Options.Create(new OpenRouterOptions { ApiKey = "test-key" }));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => client.CreateMessageAsync("system", "user", CancellationToken.None));

        Assert.Contains("did not contain usable 'content'", exception.Message);
        Assert.Contains("reasoning", exception.Message);
        Assert.Contains("finish_reason: length", exception.Message);
    }

    [Fact]
    public async Task CreateMessageAsync_RetriesTransientHttpFailures()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new HttpRequestException("temporary upstream issue", null, HttpStatusCode.ServiceUnavailable);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"content":"retry worked"}}]}""",
                    Encoding.UTF8,
                    "application/json")
            });
        });
        using var httpClient = new HttpClient(handler);
        var client = new OpenRouterMessageClient(
            httpClient,
            Options.Create(new OpenRouterOptions { ApiKey = "test-key" }));

        var result = await client.CreateMessageAsync("system", "user", CancellationToken.None);

        Assert.Equal("retry worked", result);
        Assert.Equal(2, attempts);
    }

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
