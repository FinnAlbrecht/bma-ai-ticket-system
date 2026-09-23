using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Domain.Tickets.Exceptions;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Tickets.Queries;

public class GetTicketByIdQueryHandler
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketByIdQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketDto> HandleAsync(GetTicketByIdQuery query, CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(query.TicketId, ct)
            ?? throw new TicketNotFoundException(query.TicketId);

        return TicketDto.FromDomain(ticket);
    }
}
