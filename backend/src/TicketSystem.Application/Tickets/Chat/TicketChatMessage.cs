namespace TicketSystem.Application.Tickets.Chat;

public sealed record TicketChatMessage(
    Guid Id,
    Guid TicketId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    bool IsRead);

public sealed record TicketChatNotification(
    Guid MessageId,
    Guid TicketId,
    string TicketTitle,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record TicketChatExchange(
    TicketChatMessage UserMessage,
    TicketChatMessage AssistantMessage);
