namespace TicketSystem.Domain.Tickets.Exceptions;

public class TicketAlreadyClosedException : Exception
{
    public TicketAlreadyClosedException(Guid ticketId)
        : base($"Ticket mit Id '{ticketId}' ist bereits abgeschlossen.")
    {
    }
}
