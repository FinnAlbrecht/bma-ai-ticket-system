using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Tickets.Chat;

namespace TicketSystem.Infrastructure.Persistence.Repositories;

public sealed class TicketChatMessageRepository(TicketDbContext dbContext) : ITicketChatMessageRepository
{
    public async Task<IReadOnlyList<TicketChatMessage>> GetByTicketIdAsync(
        Guid ticketId,
        CancellationToken ct = default)
    {
        var records = await dbContext.ChatMessages.AsNoTracking()
            .Where(message => message.TicketId == ticketId)
            .OrderBy(message => message.CreatedAtUtcTicks)
            .ThenBy(message => message.Id)
            .ToListAsync(ct);
        return records.Select(ToMessage).ToArray();
    }

    public async Task<IReadOnlyList<TicketChatMessage>> GetRecentByTicketIdAsync(
        Guid ticketId,
        int count,
        CancellationToken ct = default)
    {
        var records = await dbContext.ChatMessages.AsNoTracking()
            .Where(message => message.TicketId == ticketId)
            .OrderByDescending(message => message.CreatedAtUtcTicks)
            .ThenByDescending(message => message.Id)
            .Take(count)
            .ToListAsync(ct);
        return records
            .OrderBy(message => message.CreatedAtUtcTicks)
            .ThenBy(message => message.Id)
            .Select(ToMessage)
            .ToArray();
    }

    public async Task<IReadOnlyList<TicketChatNotification>> GetUnreadByOwnerAsync(
        Guid ownerId,
        CancellationToken ct = default)
    {
        var messages = await (
            from message in dbContext.ChatMessages.AsNoTracking()
            join ticket in dbContext.Tickets.AsNoTracking() on message.TicketId equals ticket.Id
            where ticket.CreatedByUserId == ownerId
                && ticket.Category != "OutOfScope"
                && ticket.Status == "Resolved"
                && message.Role == "assistant"
                && !message.IsRead
            orderby message.CreatedAtUtcTicks descending
            select new { message, ticket.Title }
        ).Take(100).ToListAsync(ct);

        return messages
            .GroupBy(item => item.message.TicketId)
            .Select(group => group.First())
            .Take(20)
            .Select(item => new TicketChatNotification(
                item.message.Id,
                item.message.TicketId,
                item.Title,
                item.message.Content,
                new DateTimeOffset(item.message.CreatedAtUtcTicks, TimeSpan.Zero)))
            .ToArray();
    }

    public async Task<TicketChatMessage> AddAsync(
        Guid ticketId,
        string role,
        string content,
        CancellationToken ct = default)
    {
        var record = new TicketChatMessageRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Role = role,
            Content = content,
            CreatedAtUtcTicks = DateTimeOffset.UtcNow.UtcDateTime.Ticks,
            IsRead = role != "assistant"
        };

        await dbContext.ChatMessages.AddAsync(record, ct);
        await dbContext.SaveChangesAsync(ct);
        return ToMessage(record);
    }

    public async Task<TicketChatExchange> AddExchangeAsync(
        Guid ticketId,
        string userMessage,
        string assistantMessage,
        CancellationToken ct = default)
    {
        var createdAt = DateTimeOffset.UtcNow.UtcDateTime.Ticks;
        var userRecord = new TicketChatMessageRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Role = "user",
            Content = userMessage,
            CreatedAtUtcTicks = createdAt,
            IsRead = true
        };
        var assistantRecord = new TicketChatMessageRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Role = "assistant",
            Content = assistantMessage,
            CreatedAtUtcTicks = createdAt + 1,
            IsRead = false
        };

        await dbContext.ChatMessages.AddRangeAsync([userRecord, assistantRecord], ct);
        await dbContext.SaveChangesAsync(ct);
        return new TicketChatExchange(ToMessage(userRecord), ToMessage(assistantRecord));
    }

    public async Task MarkTicketMessagesReadAsync(Guid ticketId, CancellationToken ct = default)
    {
        await dbContext.ChatMessages
            .Where(message => message.TicketId == ticketId && message.Role == "assistant" && !message.IsRead)
            .ExecuteUpdateAsync(update => update.SetProperty(message => message.IsRead, true), ct);
    }

    private static TicketChatMessage ToMessage(TicketChatMessageRecord record) => new(
        record.Id,
        record.TicketId,
        record.Role,
        record.Content,
        new DateTimeOffset(record.CreatedAtUtcTicks, TimeSpan.Zero),
        record.IsRead);
}
