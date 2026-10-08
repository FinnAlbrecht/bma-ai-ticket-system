namespace TicketSystem.Infrastructure.Ai.Clients;

public sealed record OpenRouterChatMessage(string Role, string Content);

public interface IOpenRouterMessageClient
{
    Task<string> CreateMessageAsync(string systemPrompt, string userPrompt, CancellationToken ct);
    Task<string> CreateChatCompletionAsync(
        string systemPrompt,
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken ct);
}
