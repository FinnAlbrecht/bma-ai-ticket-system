namespace TicketSystem.Api.Contracts;

public sealed record AuthUserDto(Guid Id, string Email, string DisplayName);
