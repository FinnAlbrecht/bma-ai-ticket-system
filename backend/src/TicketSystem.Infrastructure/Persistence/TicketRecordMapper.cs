using TicketSystem.Domain.Classification.Entities;
using TicketSystem.Domain.Classification.Enums;
using TicketSystem.Domain.Classification.ValueObjects;
using TicketSystem.Domain.Tickets.Entities;
using TicketSystem.Domain.Tickets.Enums;

namespace TicketSystem.Infrastructure.Persistence;

internal static class TicketRecordMapper
{
    public static TicketRecord ToRecord(Ticket ticket) => new()
    {
        Id = ticket.Id,
        Title = ticket.Title,
        Description = ticket.Description,
        Category = ticket.Category.ToString(),
        Status = ticket.Status.ToString(),
        CreatedAt = ticket.CreatedAt,
        ClassifiedAt = ticket.ClassifiedAt,
        ResolvedAt = ticket.ResolvedAt,
        ResolutionNotes = ticket.ResolutionNotes,
        SuggestedResolution = ticket.SuggestedResolution,
        SolutionSourceTicketId = ticket.SolutionSourceTicketId,
        CreatedByUserId = ticket.CreatedByUserId,
        CreatedByName = ticket.CreatedByName
    };

    public static Ticket ToDomain(TicketRecord record)
        => Ticket.Reconstitute(
            record.Id,
            record.Title,
            record.Description,
            Enum.Parse<TicketCategory>(record.Category),
            Enum.Parse<TicketStatus>(record.Status),
            record.CreatedAt,
            record.ClassifiedAt,
            record.ResolvedAt,
            record.ResolutionNotes,
            record.SuggestedResolution,
            record.SolutionSourceTicketId,
            record.CreatedByUserId,
            record.CreatedByName);

    public static TicketClassificationRecord ToRecord(TicketClassification classification) => new()
    {
        Id = classification.Id,
        TicketId = classification.TicketId,
        Category = classification.Category.ToString(),
        Confidence = classification.Confidence.Value,
        SuggestedSolution = classification.SuggestedSolution,
        Source = classification.Source.ToString(),
        Model = classification.Model,
        IsItRelated = classification.IsItRelated,
        DurationMilliseconds = (long)Math.Round(classification.Duration.TotalMilliseconds),
        CreatedAt = classification.CreatedAt
    };

    public static TicketClassification ToDomain(TicketClassificationRecord record) => new(
        record.TicketId,
        Enum.Parse<TicketCategory>(record.Category),
        new ClassificationConfidence(record.Confidence),
        record.SuggestedSolution,
        Enum.Parse<ClassificationSource>(record.Source),
        TimeSpan.FromMilliseconds(record.DurationMilliseconds),
        record.Model,
        record.IsItRelated,
        record.Id,
        record.CreatedAt);
}
