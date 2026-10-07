namespace TicketSystem.Application.Tickets.Commands;

public record CreateTicketCommand(string Title, string Description, Guid CreatedByUserId, string CreatedByName);
