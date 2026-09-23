using TicketSystem.Domain.Tickets.Entities;

namespace TicketSystem.Application.Tickets.Dtos;

public record TicketDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Category,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClassifiedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNotes)
{
    public static TicketDto FromDomain(Ticket ticket) => new(
        ticket.Id,
        ticket.Title,
        ticket.Description,
        ticket.Status.ToString(),
        ticket.Category.ToString(),
        ticket.CreatedAt,
        ticket.ClassifiedAt,
        ticket.ResolvedAt,
        ticket.ResolutionNotes);
}
