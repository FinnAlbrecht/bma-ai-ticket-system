namespace TicketSystem.Application.Tickets.Chat;

public interface ITicketChatMessageRepository
{
    Task<IReadOnlyList<TicketChatMessage>> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<TicketChatMessage>> GetRecentByTicketIdAsync(Guid ticketId, int count, CancellationToken ct = default);
    Task<IReadOnlyList<TicketChatNotification>> GetUnreadByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<TicketChatMessage> AddAsync(Guid ticketId, string role, string content, CancellationToken ct = default);
    Task<TicketChatExchange> AddExchangeAsync(
        Guid ticketId,
        string userMessage,
        string assistantMessage,
        CancellationToken ct = default);
    Task MarkTicketMessagesReadAsync(Guid ticketId, CancellationToken ct = default);
}
