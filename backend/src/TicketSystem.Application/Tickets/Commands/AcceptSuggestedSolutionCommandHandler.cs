using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Domain.Tickets.Exceptions;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Tickets.Commands;

public sealed class AcceptSuggestedSolutionCommandHandler(ITicketRepository ticketRepository)
{
    public async Task<TicketDto> HandleAsync(
        AcceptSuggestedSolutionCommand command,
        CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.TicketId, ct)
            ?? throw new TicketNotFoundException(command.TicketId);

        ticket.ResolveSuggestedSolution();
        await ticketRepository.UpdateAsync(ticket, ct);
        return TicketDto.FromDomain(ticket);
    }
}
