namespace TicketSystem.Application.Tickets.Dtos;

public record TicketDashboardItemDto(
    Guid Id,
    string Title,
    string Status,
    string Category,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    long? AiDurationMilliseconds);
