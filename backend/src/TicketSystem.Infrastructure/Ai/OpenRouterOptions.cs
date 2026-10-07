namespace TicketSystem.Infrastructure.Ai;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "nvidia/nemotron-3-ultra-550b-a55b:free";
    public string Endpoint { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
    public int MaxTokens { get; set; } = 1024;
    public int TimeoutSeconds { get; set; } = 60;

    public bool HasValidApiKey =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.Equals(ApiKey.Trim(), "XXXXX", StringComparison.Ordinal);
}
