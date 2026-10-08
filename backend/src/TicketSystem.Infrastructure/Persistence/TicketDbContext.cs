using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Infrastructure.Persistence;

public sealed class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<TicketRecord> Tickets => Set<TicketRecord>();
    public DbSet<TicketClassificationRecord> Classifications => Set<TicketClassificationRecord>();
    public DbSet<TicketChatMessageRecord> ChatMessages => Set<TicketChatMessageRecord>();
    public DbSet<UserRecord> Users => Set<UserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketRecord>(entity =>
        {
            entity.HasKey(ticket => ticket.Id);
            entity.Property(ticket => ticket.Title).HasMaxLength(4000).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(4000).IsRequired();
            entity.Property(ticket => ticket.Category).HasMaxLength(40).IsRequired();
            entity.Property(ticket => ticket.Status).HasMaxLength(40).IsRequired();
            entity.Property(ticket => ticket.CreatedByName).HasMaxLength(100);
            entity.Property(ticket => ticket.CreatedAt)
                .HasConversion(value => value.UtcDateTime.Ticks, value => new DateTimeOffset(value, TimeSpan.Zero));
            entity.Property(ticket => ticket.ClassifiedAt)
                .HasConversion(value => value.HasValue ? value.Value.UtcDateTime.Ticks : (long?)null, value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
            entity.Property(ticket => ticket.ResolvedAt)
                .HasConversion(value => value.HasValue ? value.Value.UtcDateTime.Ticks : (long?)null, value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
            entity.HasIndex(ticket => ticket.CreatedAt);
            entity.HasIndex(ticket => ticket.Status);
            entity.HasIndex(ticket => ticket.CreatedByUserId);
        });

        modelBuilder.Entity<UserRecord>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<TicketClassificationRecord>(entity =>
        {
            entity.HasKey(classification => classification.Id);
            entity.Property(classification => classification.Category).HasMaxLength(40).IsRequired();
            entity.Property(classification => classification.Source).HasMaxLength(40).IsRequired();
            entity.Property(classification => classification.Model).HasMaxLength(200).IsRequired();
            entity.Property(classification => classification.SuggestedSolution).HasMaxLength(8000).IsRequired();
            entity.Property(classification => classification.CreatedAt)
                .HasConversion(value => value.UtcDateTime.Ticks, value => new DateTimeOffset(value, TimeSpan.Zero));
            entity.HasIndex(classification => new { classification.TicketId, classification.CreatedAt });
        });

        modelBuilder.Entity<TicketChatMessageRecord>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Role).HasMaxLength(20).IsRequired();
            entity.Property(message => message.Content).HasMaxLength(8000).IsRequired();
            entity.HasIndex(message => new { message.TicketId, message.CreatedAtUtcTicks });
        });
    }
}
