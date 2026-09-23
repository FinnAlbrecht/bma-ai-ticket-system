using System.Collections.Concurrent;
using TicketSystem.Domain.Tickets.Entities;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Infrastructure.Persistence.Repositories;

/// <summary>
/// Vorläufige Persistenz für die frühe Entwicklungsphase. Kann später ohne Änderungen an
/// Domain/Application durch eine EF-Core-Implementierung (z. B. SQLite) ersetzt werden,
/// da nur gegen ITicketRepository programmiert wird.
/// </summary>
public class InMemoryTicketRepository : ITicketRepository
{
    private readonly ConcurrentDictionary<Guid, Ticket> _tickets = new();

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_tickets.TryGetValue(id, out var ticket) ? ticket : null);

    public Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Ticket>>(_tickets.Values.OrderByDescending(t => t.CreatedAt).ToList());

    public Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        _tickets[ticket.Id] = ticket;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        _tickets[ticket.Id] = ticket;
        return Task.CompletedTask;
    }
}
