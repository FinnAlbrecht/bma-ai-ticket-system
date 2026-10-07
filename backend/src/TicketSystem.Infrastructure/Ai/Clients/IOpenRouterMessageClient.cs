namespace TicketSystem.Infrastructure.Ai.Clients;

public interface IOpenRouterMessageClient
{
    Task<string> CreateMessageAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}
