using System.Net;
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
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CreateMessageOnceAsync(systemPrompt, userPrompt, ct);
            }
            catch (HttpRequestException ex) when (ShouldRetry(ex, attempt))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), ct);
            }
        }
    }

    private async Task<string> CreateMessageOnceAsync(string systemPrompt, string userPrompt, CancellationToken ct)
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

        if (document.RootElement.TryGetProperty("error", out var errorElement))
        {
            var errorMessage = errorElement.TryGetProperty("message", out var messageElement)
                ? messageElement.ToString()
                : errorElement.ToString();
            throw new HttpRequestException($"OpenRouter returned an upstream error: {errorMessage}");
        }

        if (!document.RootElement.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            throw new InvalidDataException(
                $"Successful OpenRouter response did not contain a non-empty 'choices' array. " +
                $"Top-level fields: {GetPropertyNames(document.RootElement)}.");
        }

        if (!choices[0].TryGetProperty("message", out var message)
            || message.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"Successful OpenRouter response choice did not contain a 'message' object. " +
                $"Choice fields: {GetPropertyNames(choices[0])}.");
        }

        var content = GetMessageContent(message);
        if (content is null)
        {
            var finishReason = choices[0].TryGetProperty("finish_reason", out var finishReasonElement)
                ? finishReasonElement.ToString()
                : "not provided";
            throw new InvalidDataException(
                $"Successful OpenRouter response message did not contain usable 'content'. " +
                $"Message fields: {GetPropertyNames(message)}; finish_reason: {finishReason}.");
        }

        return content;
    }

    private static bool ShouldRetry(HttpRequestException ex, int attempt)
    {
        if (attempt >= 3)
            return false;

        var statusCode = ex.StatusCode;
        return statusCode is null
            || statusCode.Value is HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests
                or HttpStatusCode.InternalServerError
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout;
    }

    private static string? GetMessageContent(JsonElement message)
    {
        if (message.TryGetProperty("content", out var content))
        {
            if (content.ValueKind == JsonValueKind.String)
                return content.GetString();

            if (content.ValueKind == JsonValueKind.Array)
            {
                var segments = content.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.Object)
                    .SelectMany(item => item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String
                        ? new[] { text.GetString() }
                        : Array.Empty<string>())
                    .Where(value => !string.IsNullOrEmpty(value))
                    .ToArray();

                if (segments.Length > 0)
                    return string.Join(Environment.NewLine, segments);
            }

            if (content.ValueKind == JsonValueKind.Object
                && content.TryGetProperty("text", out var text)
                && text.ValueKind == JsonValueKind.String)
            {
                return text.GetString();
            }
        }

        return null;
    }

    private static string GetPropertyNames(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? string.Join(", ", element.EnumerateObject().Select(property => property.Name))
            : element.ValueKind.ToString();
}
