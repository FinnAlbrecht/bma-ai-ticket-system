using TicketSystem.Domain.Classification.Entities;

namespace TicketSystem.Domain.Classification.Repositories;

public interface ITicketClassificationRepository
{
    Task AddAsync(TicketClassification classification, CancellationToken ct = default);
    Task<IReadOnlyList<TicketClassification>> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
}
