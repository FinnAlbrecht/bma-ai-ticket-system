namespace TicketSystem.Infrastructure.Ai.Clients;

public interface IAnthropicMessageClient
{
    Task<string> CreateMessageAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}
