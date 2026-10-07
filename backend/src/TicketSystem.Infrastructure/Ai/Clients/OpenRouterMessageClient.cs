using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketSystem.Infrastructure.Ai;

namespace TicketSystem.Infrastructure.Ai.Clients;

public sealed class OpenRouterMessageClient(
    HttpClient httpClient,
    IOptions<OpenRouterOptions> options) : IOpenRouterMessageClient
{
    public async Task<string> CreateMessageAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var settings = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = settings.Model,
            max_tokens = settings.MaxTokens,
            temperature = 0,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        });

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!document.RootElement.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0
            || !choices[0].TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content))
            throw new HttpRequestException("OpenRouter returned an invalid chat completion response.");

        if (content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? string.Empty;

        if (content.ValueKind == JsonValueKind.Array)
        {
            return string.Join(
                Environment.NewLine,
                content.EnumerateArray()
                    .Where(item =>
                        item.ValueKind == JsonValueKind.Object
                        && item.TryGetProperty("text", out var text)
                        && text.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetProperty("text").GetString())
                    .Where(text => text is not null));
        }

        throw new HttpRequestException("OpenRouter returned an unsupported message content format.");
    }
}
