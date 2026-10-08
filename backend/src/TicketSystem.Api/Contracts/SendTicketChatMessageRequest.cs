using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Api.Contracts;

public sealed record SendTicketChatMessageRequest(
    [param: Required, StringLength(4000, MinimumLength = 1)] string Message);
