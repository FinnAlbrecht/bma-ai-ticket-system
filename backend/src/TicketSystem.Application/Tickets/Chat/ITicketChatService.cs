namespace TicketSystem.Application.Tickets.Chat;

public interface ITicketChatService
{
    Task<IReadOnlyList<TicketChatMessage>> GetHistoryAsync(Guid ticketId, CancellationToken ct = default);
    Task<TicketChatExchange> SendMessageAsync(Guid ticketId, string message, CancellationToken ct = default);
    Task<IReadOnlyList<TicketChatNotification>> GetUnreadMessagesAsync(Guid ownerId, CancellationToken ct = default);
    Task MarkMessagesReadAsync(Guid ticketId, CancellationToken ct = default);
}
