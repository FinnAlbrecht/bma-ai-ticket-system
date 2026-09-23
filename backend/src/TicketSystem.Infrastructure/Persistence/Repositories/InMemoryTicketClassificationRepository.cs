using System.Collections.Concurrent;
using TicketSystem.Domain.Classification.Entities;
using TicketSystem.Domain.Classification.Repositories;

namespace TicketSystem.Infrastructure.Persistence.Repositories;

public class InMemoryTicketClassificationRepository : ITicketClassificationRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<TicketClassification>> _classificationsByTicket = new();

    public Task AddAsync(TicketClassification classification, CancellationToken ct = default)
    {
        var bag = _classificationsByTicket.GetOrAdd(classification.TicketId, _ => new ConcurrentBag<TicketClassification>());
        bag.Add(classification);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TicketClassification>> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default)
    {
        var result = _classificationsByTicket.TryGetValue(ticketId, out var bag)
            ? bag.OrderByDescending(c => c.CreatedAt).ToList()
            : new List<TicketClassification>();

        return Task.FromResult<IReadOnlyList<TicketClassification>>(result);
    }
}
