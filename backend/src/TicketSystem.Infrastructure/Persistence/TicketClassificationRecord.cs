namespace TicketSystem.Infrastructure.Persistence;

public sealed class TicketClassificationRecord
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string SuggestedSolution { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsItRelated { get; set; }
    public long DurationMilliseconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
