using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Tickets.Entities;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Infrastructure.Persistence.Repositories;

public sealed class SqliteTicketRepository(TicketDbContext dbContext) : ITicketRepository
{
    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var record = await dbContext.Tickets.AsNoTracking().SingleOrDefaultAsync(ticket => ticket.Id == id, ct);
        return record is null ? null : TicketRecordMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default)
    {
        var records = await dbContext.Tickets.AsNoTracking()
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(ct);
        return records.Select(TicketRecordMapper.ToDomain).ToArray();
    }

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        await dbContext.Tickets.AddAsync(TicketRecordMapper.ToRecord(ticket), ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        var record = TicketRecordMapper.ToRecord(ticket);
        var trackedRecord = dbContext.Tickets.Local.SingleOrDefault(existing => existing.Id == record.Id);
        if (trackedRecord is null)
        {
            dbContext.Tickets.Update(record);
        }
        else
        {
            dbContext.Entry(trackedRecord).CurrentValues.SetValues(record);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAllAsync(CancellationToken ct = default)
    {
        var classifications = await dbContext.Classifications.ToListAsync(ct);
        dbContext.Classifications.RemoveRange(classifications);

        var tickets = await dbContext.Tickets.ToListAsync(ct);
        dbContext.Tickets.RemoveRange(tickets);

        await dbContext.SaveChangesAsync(ct);
    }
}
