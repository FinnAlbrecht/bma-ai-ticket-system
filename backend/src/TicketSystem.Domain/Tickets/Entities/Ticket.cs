using TicketSystem.Domain.Tickets.Enums;
using TicketSystem.Domain.Tickets.Exceptions;

namespace TicketSystem.Domain.Tickets.Entities;

public class Ticket
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketCategory Category { get; private set; } = TicketCategory.Unclassified;
    public TicketStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClassifiedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public string? SuggestedResolution { get; private set; }
    public Guid? SolutionSourceTicketId { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }

    public Ticket(string title, string description, Guid? createdByUserId = null, string? createdByName = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title darf nicht leer sein.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description darf nicht leer sein.", nameof(description));
        if (title.Length > 4000)
            throw new ArgumentException("Title darf höchstens 4000 Zeichen enthalten.", nameof(title));
        if (description.Length > 4000)
            throw new ArgumentException("Description darf höchstens 4000 Zeichen enthalten.", nameof(description));

        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Status = TicketStatus.New;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName;
    }

    public static Ticket Reconstitute(
        Guid id,
        string title,
        string description,
        TicketCategory category,
        TicketStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? classifiedAt,
        DateTimeOffset? resolvedAt,
        string? resolutionNotes,
        string? suggestedResolution,
        Guid? solutionSourceTicketId,
        Guid? createdByUserId = null,
        string? createdByName = null)
    {
        var ticket = new Ticket(title, description)
        {
            Id = id,
            Category = category,
            Status = status,
            CreatedAt = createdAt,
            ClassifiedAt = classifiedAt,
            ResolvedAt = resolvedAt,
            ResolutionNotes = resolutionNotes,
            SuggestedResolution = suggestedResolution,
            SolutionSourceTicketId = solutionSourceTicketId,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName
        };

        return ticket;
    }

    public void ApplyClassification(TicketCategory category)
    {
        Category = category;
        Status = TicketStatus.Classified;
        ClassifiedAt = DateTimeOffset.UtcNow;
        SuggestedResolution = null;
        SolutionSourceTicketId = null;
    }

    public void StartProcessing()
    {
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new TicketAlreadyClosedException(Id);

        Status = TicketStatus.InProgress;
    }

    public void Resolve(string resolutionNotes)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new ArgumentException("ResolutionNotes darf nicht leer sein.", nameof(resolutionNotes));
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new TicketAlreadyClosedException(Id);

        Status = TicketStatus.Resolved;
        ResolutionNotes = resolutionNotes;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void ResolveAsOutOfScope()
    {
        if (Category != TicketCategory.OutOfScope)
            throw new ArgumentException("Nur Tickets ohne IT-Bezug können so abgeschlossen werden.", nameof(Category));

        Resolve("Als Ticket ohne IT-Bezug vom Nutzer als gelöst markiert.");
    }

    public void SuggestSimilarResolution(TicketCategory category, string solution, Guid sourceTicketId)
    {
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new TicketAlreadyClosedException(Id);
        if (category is TicketCategory.Unclassified or TicketCategory.OutOfScope)
            throw new ArgumentException("Für nicht-IT-Tickets kann keine frühere Lösung übernommen werden.", nameof(category));
        if (string.IsNullOrWhiteSpace(solution))
            throw new ArgumentException("Eine vorgeschlagene Lösung darf nicht leer sein.", nameof(solution));
        if (sourceTicketId == Id)
            throw new ArgumentException("Ein Ticket kann nicht auf seine eigene Lösung verweisen.", nameof(sourceTicketId));

        Category = category;
        SuggestedResolution = solution;
        SolutionSourceTicketId = sourceTicketId;
        ClassifiedAt = DateTimeOffset.UtcNow;
        Status = TicketStatus.AnswerFound;
    }

    public void ResolveSuggestedSolution()
    {
        if (string.IsNullOrWhiteSpace(SuggestedResolution))
            throw new ArgumentException("Für dieses Ticket liegt keine wiederverwendbare Lösung vor.", nameof(SuggestedResolution));

        Resolve(SuggestedResolution);
    }
}
