using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Domain.Tickets.Entities;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Tickets.Commands;

public class CreateTicketCommandHandler
{
    private readonly ITicketRepository _ticketRepository;

    public CreateTicketCommandHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketDto> HandleAsync(CreateTicketCommand command, CancellationToken ct = default)
    {
        var ticket = new Ticket(command.Title, command.Description);
        await _ticketRepository.AddAsync(ticket, ct);
        return TicketDto.FromDomain(ticket);
    }
}
