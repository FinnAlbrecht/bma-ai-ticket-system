using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Api.Contracts;

public sealed record LoginRequest(
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, StringLength(128, MinimumLength = 1)] string Password);
