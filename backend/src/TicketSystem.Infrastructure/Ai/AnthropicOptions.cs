namespace TicketSystem.Infrastructure.Ai;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-5-5";
    public int MaxTokens { get; set; } = 1024;
    public int TimeoutSeconds { get; set; } = 60;

    public bool HasValidApiKey =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.Equals(ApiKey.Trim(), "XXXXX", StringComparison.Ordinal);
}
