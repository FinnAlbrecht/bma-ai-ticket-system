namespace TicketSystem.Domain.Tickets.Exceptions;

public class TicketNotFoundException : Exception
{
    public TicketNotFoundException(Guid ticketId)
        : base($"Ticket mit Id '{ticketId}' wurde nicht gefunden.")
    {
    }
}
