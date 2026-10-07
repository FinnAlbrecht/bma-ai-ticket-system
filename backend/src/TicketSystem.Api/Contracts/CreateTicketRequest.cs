using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Api.Contracts;

public record CreateTicketRequest(
    [param: Required, StringLength(4000, MinimumLength = 1)] string Title,
    [param: Required, StringLength(4000, MinimumLength = 1)] string Description);
