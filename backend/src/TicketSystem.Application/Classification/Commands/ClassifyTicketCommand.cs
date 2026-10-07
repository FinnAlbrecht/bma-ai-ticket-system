namespace TicketSystem.Application.Classification.Commands;

public record ClassifyTicketCommand(Guid TicketId, string? Provider = null);
