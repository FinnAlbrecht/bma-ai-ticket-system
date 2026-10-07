using TicketSystem.Domain.Tickets.Enums;
using TicketSystem.Domain.Classification.Enums;

namespace TicketSystem.Application.Common.Interfaces;

/// <summary>
/// Port für die Ticket-Klassifizierung. Die Implementierungen liegen in TicketSystem.Infrastructure.
/// </summary>
public interface IClassificationService
{
    Task<ClassificationOutcome> ClassifyAsync(string title, string description, CancellationToken ct = default);
}

public interface IClassificationServiceResolver
{
    IClassificationService Resolve(string? provider);
}

public record ClassificationOutcome(
    TicketCategory Category,
    double Confidence,
    string SuggestedSolution,
    TimeSpan Duration,
    ClassificationSource Source,
    string Model,
    bool IsItRelated);
