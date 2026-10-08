using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicketSystem.Domain.Tickets.Entities;
using TicketSystem.Infrastructure.Ai;
using TicketSystem.Infrastructure.Ai.Clients;
using TicketSystem.Infrastructure.Persistence;
using TicketSystem.Infrastructure.Persistence.Repositories;

namespace TicketSystem.Tests;

public sealed class TicketChatServiceTests
{
    [Fact]
    public async Task SendMessageAsync_PersistsExchangeAndSendsConversationContextToOpenRouter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TicketDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var database = new TicketDbContext(options);
        await database.Database.EnsureCreatedAsync();

        var ticketRepository = new SqliteTicketRepository(database);
        var ownerId = Guid.NewGuid();
        var ticket = new Ticket("WLAN-Verbindung", "Mein Laptop verliert die WLAN-Verbindung.", ownerId, "David");
        await ticketRepository.AddAsync(ticket);

        var messageRepository = new TicketChatMessageRepository(database);
        await messageRepository.AddAsync(ticket.Id, "user", "Das Problem begann heute.");
        await messageRepository.AddAsync(ticket.Id, "assistant", "Hast du den Router neu gestartet?");
        var openRouter = new RecordingOpenRouterMessageClient("Prüfe bitte, ob andere Geräte verbunden sind.");
        var service = new TicketChatService(
            ticketRepository,
            messageRepository,
            openRouter,
            Options.Create(new OpenRouterOptions { ApiKey = "test-key" }),
            NullLogger<TicketChatService>.Instance);

        var exchange = await service.SendMessageAsync(ticket.Id, "Ja, andere Geräte funktionieren.");

        var history = await messageRepository.GetByTicketIdAsync(ticket.Id);
        Assert.Equal("user", exchange.UserMessage.Role);
        Assert.Equal("assistant", exchange.AssistantMessage.Role);
        Assert.False(exchange.AssistantMessage.IsRead);
        Assert.Equal(4, history.Count);
        Assert.Equal("Ja, andere Geräte funktionieren.", openRouter.Messages[^1].Content);
        Assert.Equal("assistant", openRouter.Messages[^2].Role);
        Assert.Contains("WLAN-Verbindung", openRouter.SystemPrompt);
        Assert.Contains("Mein Laptop verliert die WLAN-Verbindung.", openRouter.SystemPrompt);
    }

    private sealed class RecordingOpenRouterMessageClient(string answer) : IOpenRouterMessageClient
    {
        public string SystemPrompt { get; private set; } = string.Empty;
        public IReadOnlyList<OpenRouterChatMessage> Messages { get; private set; } = [];

        public Task<string> CreateMessageAsync(string systemPrompt, string userPrompt, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<string> CreateChatCompletionAsync(
            string systemPrompt,
            IReadOnlyList<OpenRouterChatMessage> messages,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            SystemPrompt = systemPrompt;
            Messages = messages;
            return Task.FromResult(answer);
        }
    }
}
