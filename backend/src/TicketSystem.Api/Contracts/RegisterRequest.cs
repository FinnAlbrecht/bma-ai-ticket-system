using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Api.Contracts;

public sealed record RegisterRequest(
    [param: Required, StringLength(100, MinimumLength = 2)] string DisplayName,
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, MinLength(10), StringLength(128)] string Password);
