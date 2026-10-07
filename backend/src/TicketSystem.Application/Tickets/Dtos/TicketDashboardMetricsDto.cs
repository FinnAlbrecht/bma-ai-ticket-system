namespace TicketSystem.Application.Tickets.Dtos;

public record TicketDashboardMetricsDto(
    int TotalTickets,
    int InProgressTickets,
    int ResolvedTickets,
    long AverageAiDurationMilliseconds,
    long TotalAiDurationMilliseconds,
    long AverageResolutionMilliseconds,
    long TotalResolutionMilliseconds,
    IReadOnlyList<TicketDashboardItemDto> Tickets);
