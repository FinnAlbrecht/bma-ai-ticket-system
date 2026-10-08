using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Api.Contracts;

public sealed record DeleteAllTicketsRequest(
    [param: Required, StringLength(256, MinimumLength = 1)] string Password);
