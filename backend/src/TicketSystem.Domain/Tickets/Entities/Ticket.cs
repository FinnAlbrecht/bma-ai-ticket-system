using TicketSystem.Domain.Tickets.Enums;

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

    public Ticket(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title darf nicht leer sein.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description darf nicht leer sein.", nameof(description));

        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Status = TicketStatus.New;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyClassification(TicketCategory category)
    {
        Category = category;
        Status = TicketStatus.Classified;
        ClassifiedAt = DateTimeOffset.UtcNow;
    }

    public void Resolve(string resolutionNotes)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new ArgumentException("ResolutionNotes darf nicht leer sein.", nameof(resolutionNotes));

        Status = TicketStatus.Resolved;
        ResolutionNotes = resolutionNotes;
        ResolvedAt = DateTimeOffset.UtcNow;
    }
}
