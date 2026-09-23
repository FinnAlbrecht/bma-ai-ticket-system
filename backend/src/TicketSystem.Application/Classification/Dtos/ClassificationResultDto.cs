namespace TicketSystem.Application.Classification.Dtos;

public record ClassificationResultDto(
    Guid TicketId,
    string Category,
    double Confidence,
    string SuggestedSolution,
    string Source,
    TimeSpan Duration,
    DateTimeOffset CreatedAt);
