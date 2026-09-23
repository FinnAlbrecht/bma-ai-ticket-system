using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Tickets.Queries;

public class GetAllTicketsQueryHandler
{
    private readonly ITicketRepository _ticketRepository;

    public GetAllTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IReadOnlyList<TicketDto>> HandleAsync(GetAllTicketsQuery query, CancellationToken ct = default)
    {
        var tickets = await _ticketRepository.GetAllAsync(ct);
        return tickets.Select(TicketDto.FromDomain).ToList();
    }
}
