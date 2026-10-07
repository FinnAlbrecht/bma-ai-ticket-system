using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Domain.Classification.Enums;
using TicketSystem.Domain.Classification.Repositories;
using TicketSystem.Domain.Tickets.Enums;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Tickets.Queries;

public sealed class GetTicketDashboardMetricsQueryHandler(
    ITicketRepository ticketRepository,
    ITicketClassificationRepository classificationRepository)
{
    public async Task<TicketDashboardMetricsDto> HandleAsync(
        GetTicketDashboardMetricsQuery query,
        CancellationToken ct = default)
    {
        var tickets = await ticketRepository.GetAllAsync(ct);
        var classifications = await classificationRepository.GetAllAsync(ct);

        var latestAiClassifications = classifications
            .Where(classification => classification.Source == ClassificationSource.Ai)
            .GroupBy(classification => classification.TicketId)
            .ToDictionary(group => group.Key, group => group.MaxBy(classification => classification.CreatedAt)!);

        var latestAiDurations = latestAiClassifications.Values
            .Select(classification => classification.Duration)
            .Select(duration => Math.Max(0, (long)Math.Round(duration.TotalMilliseconds)))
            .ToArray();

        var dashboardTickets = tickets
            .Select(ticket => new TicketDashboardItemDto(
                ticket.Id,
                ticket.Title,
                ticket.Status.ToString(),
                ticket.Category.ToString(),
                ticket.CreatedAt,
                ticket.ResolvedAt,
                latestAiClassifications.TryGetValue(ticket.Id, out var classification)
                    ? Math.Max(0, (long)Math.Round(classification.Duration.TotalMilliseconds))
                    : null))
            .ToArray();

        var resolutionDurations = tickets
            .Where(ticket => (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed) && ticket.ResolvedAt.HasValue)
            .Select(ticket => Math.Max(0, (long)Math.Round((ticket.ResolvedAt!.Value - ticket.CreatedAt).TotalMilliseconds)))
            .ToArray();

        return new TicketDashboardMetricsDto(
            tickets.Count,
            tickets.Count(ticket =>
                ticket.Category != TicketCategory.OutOfScope
                && (ticket.Status is TicketStatus.InProgress or TicketStatus.AnswerFound)),
            resolutionDurations.Length,
            Average(latestAiDurations),
            latestAiDurations.Sum(),
            Average(resolutionDurations),
            resolutionDurations.Sum(),
            dashboardTickets);
    }

    private static long Average(long[] durations) =>
        durations.Length == 0 ? 0 : (long)Math.Round(durations.Average());
}
