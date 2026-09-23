using TicketSystem.Domain.Tickets.Entities;

namespace TicketSystem.Domain.Tickets.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);
}
