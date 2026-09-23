using TicketSystem.Domain.Classification.Enums;
using TicketSystem.Domain.Classification.ValueObjects;
using TicketSystem.Domain.Tickets.Enums;

namespace TicketSystem.Domain.Classification.Entities;

/// <summary>
/// Eine einzelne Klassifizierung eines Tickets (durch KI oder Mensch). Ein Ticket kann
/// mehrfach klassifiziert werden, damit der Vergleich KI vs. menschlicher Support (siehe
/// Projektvereinbarung, Methode 2) auf denselben Tickets nachvollziehbar bleibt.
/// </summary>
public class TicketClassification
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public TicketCategory Category { get; private set; }
    public ClassificationConfidence Confidence { get; private set; }
    public string SuggestedSolution { get; private set; } = string.Empty;
    public ClassificationSource Source { get; private set; }
    public TimeSpan Duration { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public TicketClassification(
        Guid ticketId,
        TicketCategory category,
        ClassificationConfidence confidence,
        string suggestedSolution,
        ClassificationSource source,
        TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(suggestedSolution))
            throw new ArgumentException("SuggestedSolution darf nicht leer sein.", nameof(suggestedSolution));

        Id = Guid.NewGuid();
        TicketId = ticketId;
        Category = category;
        Confidence = confidence;
        SuggestedSolution = suggestedSolution;
        Source = source;
        Duration = duration;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
