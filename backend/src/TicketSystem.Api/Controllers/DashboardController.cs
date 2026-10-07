using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketSystem.Application.Tickets.Queries;

namespace TicketSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(GetTicketDashboardMetricsQueryHandler metricsHandler) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
        => Ok(await metricsHandler.HandleAsync(new GetTicketDashboardMetricsQuery(), ct));
}
