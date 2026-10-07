using TicketSystem.Domain.Classification.Entities;

namespace TicketSystem.Application.Classification.Dtos;

public record TicketClassificationDto(
    Guid Id,
    string Category,
    double Confidence,
    string SuggestedSolution,
    string Source,
    string Model,
    bool IsItRelated,
    TimeSpan Duration,
    DateTimeOffset CreatedAt)
{
    public static TicketClassificationDto FromDomain(TicketClassification classification) => new(
        classification.Id,
        classification.Category.ToString(),
        classification.Confidence.Value,
        classification.SuggestedSolution,
        classification.Source.ToString(),
        classification.Model,
        classification.IsItRelated,
        classification.Duration,
        classification.CreatedAt);
}
