using TicketSystem.Application.Classification.Dtos;
using TicketSystem.Domain.Classification.Repositories;
using TicketSystem.Domain.Tickets.Exceptions;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Classification.Queries;

public sealed class GetTicketClassificationHistoryQueryHandler(
    ITicketRepository ticketRepository,
    ITicketClassificationRepository classificationRepository)
{
    public async Task<IReadOnlyList<TicketClassificationDto>> HandleAsync(
        GetTicketClassificationHistoryQuery query,
        CancellationToken ct = default)
    {
        if (await ticketRepository.GetByIdAsync(query.TicketId, ct) is null)
            throw new TicketNotFoundException(query.TicketId);

        var history = await classificationRepository.GetByTicketIdAsync(query.TicketId, ct);
        return history.Select(TicketClassificationDto.FromDomain).ToList();
    }
}
