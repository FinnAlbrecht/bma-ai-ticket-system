using TicketSystem.Domain.Tickets.Enums;

namespace TicketSystem.Application.Common.Interfaces;

/// <summary>
/// Port für die Ticket-Klassifizierung. Die tatsächliche Implementierung (Anthropic Claude API
/// oder OpenAI API, siehe Projektvereinbarung 2.4) liegt in TicketSystem.Infrastructure.
/// </summary>
public interface IClassificationService
{
    Task<ClassificationOutcome> ClassifyAsync(string title, string description, CancellationToken ct = default);
}

public record ClassificationOutcome(TicketCategory Category, double Confidence, string SuggestedSolution, TimeSpan Duration);
