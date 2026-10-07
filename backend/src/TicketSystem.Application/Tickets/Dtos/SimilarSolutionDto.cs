namespace TicketSystem.Application.Tickets.Dtos;

public record SimilarSolutionDto(
    bool Found,
    Guid? SourceTicketId,
    string? SourceTicketTitle,
    string? Category,
    string? SuggestedSolution,
    double? Similarity,
    TicketDto Ticket);
