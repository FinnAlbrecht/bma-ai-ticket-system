namespace TicketSystem.Infrastructure.Persistence;

public sealed class TicketChatMessageRecord
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long CreatedAtUtcTicks { get; set; }
    public bool IsRead { get; set; }
}
