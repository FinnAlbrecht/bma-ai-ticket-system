using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Domain.Tickets.Exceptions;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Tickets.Commands;

public sealed class ResolveOutOfScopeTicketCommandHandler(ITicketRepository ticketRepository)
{
    public async Task<TicketDto> HandleAsync(
        ResolveOutOfScopeTicketCommand command,
        CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.TicketId, ct)
            ?? throw new TicketNotFoundException(command.TicketId);

        ticket.ResolveAsOutOfScope();
        await ticketRepository.UpdateAsync(ticket, ct);
        return TicketDto.FromDomain(ticket);
    }
}
