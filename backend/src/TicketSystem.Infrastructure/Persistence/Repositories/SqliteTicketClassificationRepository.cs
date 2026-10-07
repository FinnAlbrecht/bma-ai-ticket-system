using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Classification.Entities;
using TicketSystem.Domain.Classification.Repositories;

namespace TicketSystem.Infrastructure.Persistence.Repositories;

public sealed class SqliteTicketClassificationRepository(TicketDbContext dbContext) : ITicketClassificationRepository
{
    public async Task AddAsync(TicketClassification classification, CancellationToken ct = default)
    {
        await dbContext.Classifications.AddAsync(TicketRecordMapper.ToRecord(classification), ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TicketClassification>> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default)
    {
        var records = await dbContext.Classifications.AsNoTracking()
            .Where(classification => classification.TicketId == ticketId)
            .OrderByDescending(classification => classification.CreatedAt)
            .ToListAsync(ct);
        return records.Select(TicketRecordMapper.ToDomain).ToArray();
    }

    public async Task<IReadOnlyList<TicketClassification>> GetAllAsync(CancellationToken ct = default)
    {
        var records = await dbContext.Classifications.AsNoTracking()
            .OrderByDescending(classification => classification.CreatedAt)
            .ToListAsync(ct);
        return records.Select(TicketRecordMapper.ToDomain).ToArray();
    }
}
