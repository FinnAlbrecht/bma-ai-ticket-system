using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Infrastructure.Persistence;

public static class TicketDatabaseInitializer
{
    public static async Task InitializeAsync(TicketDbContext database, CancellationToken ct = default)
    {
        await database.Database.EnsureCreatedAsync(ct);
        await EnsureChatMessagesTableAsync(database, ct);
        if (!database.Database.IsSqlite())
            return;

        await database.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "Users" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                "Email" TEXT NOT NULL,
                "NormalizedEmail" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "CreatedAtUtcTicks" INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedEmail"
                ON "Users" ("NormalizedEmail");
            """,
            ct);

        await EnsureTicketColumnAsync(database, "CreatedByUserId", ct);
        await EnsureTicketColumnAsync(database, "CreatedByName", ct);
        await database.Database.ExecuteSqlRawAsync(
            """CREATE INDEX IF NOT EXISTS "IX_Tickets_CreatedByUserId" ON "Tickets" ("CreatedByUserId");""",
            ct);
    }

    private static async Task EnsureChatMessagesTableAsync(TicketDbContext database, CancellationToken ct)
    {
        var sql = database.Database.IsSqlite()
            ? """
              CREATE TABLE IF NOT EXISTS "ChatMessages" (
                  "Id" TEXT NOT NULL CONSTRAINT "PK_ChatMessages" PRIMARY KEY,
                  "TicketId" TEXT NOT NULL,
                  "Role" TEXT NOT NULL,
                  "Content" TEXT NOT NULL,
                  "CreatedAtUtcTicks" INTEGER NOT NULL,
                  "IsRead" INTEGER NOT NULL
              );
              CREATE INDEX IF NOT EXISTS "IX_ChatMessages_TicketId_CreatedAtUtcTicks"
                  ON "ChatMessages" ("TicketId", "CreatedAtUtcTicks");
              """
            : """
              CREATE TABLE IF NOT EXISTS "ChatMessages" (
                  "Id" uuid NOT NULL CONSTRAINT "PK_ChatMessages" PRIMARY KEY,
                  "TicketId" uuid NOT NULL,
                  "Role" character varying(20) NOT NULL,
                  "Content" character varying(8000) NOT NULL,
                  "CreatedAtUtcTicks" bigint NOT NULL,
                  "IsRead" boolean NOT NULL
              );
              CREATE INDEX IF NOT EXISTS "IX_ChatMessages_TicketId_CreatedAtUtcTicks"
                  ON "ChatMessages" ("TicketId", "CreatedAtUtcTicks");
              """;

        await database.Database.ExecuteSqlRawAsync(sql, ct);
    }

    private static async Task EnsureTicketColumnAsync(
        TicketDbContext database,
        string columnName,
        CancellationToken ct)
    {
        var connection = database.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """PRAGMA table_info("Tickets");""";
            await using var reader = await command.ExecuteReaderAsync(ct);
            var found = false;
            while (await reader.ReadAsync(ct))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                await reader.DisposeAsync();
                if (columnName is not ("CreatedByUserId" or "CreatedByName"))
                    throw new InvalidOperationException($"Unerwartete Ticketspalte: {columnName}");

                await using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = $"""ALTER TABLE "Tickets" ADD COLUMN "{columnName}" TEXT NULL;""";
                await alterCommand.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
