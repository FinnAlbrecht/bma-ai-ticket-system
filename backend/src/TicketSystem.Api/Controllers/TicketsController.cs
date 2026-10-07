using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Contracts;
using TicketSystem.Application.Classification.Commands;
using TicketSystem.Application.Classification.Queries;
using TicketSystem.Application.Common.Exceptions;
using TicketSystem.Application.Tickets.Commands;
using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Application.Tickets.Queries;
using TicketSystem.Domain.Tickets.Exceptions;

namespace TicketSystem.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly CreateTicketCommandHandler _createTicketHandler;
    private readonly GetAllTicketsQueryHandler _getAllTicketsHandler;
    private readonly GetTicketByIdQueryHandler _getTicketByIdHandler;
    private readonly ClassifyTicketCommandHandler _classifyTicketHandler;
    private readonly GetTicketClassificationHistoryQueryHandler _classificationHistoryHandler;

    public TicketsController(
        CreateTicketCommandHandler createTicketHandler,
        GetAllTicketsQueryHandler getAllTicketsHandler,
        GetTicketByIdQueryHandler getTicketByIdHandler,
        ClassifyTicketCommandHandler classifyTicketHandler,
        GetTicketClassificationHistoryQueryHandler classificationHistoryHandler)
    {
        _createTicketHandler = createTicketHandler;
        _getAllTicketsHandler = getAllTicketsHandler;
        _getTicketByIdHandler = getTicketByIdHandler;
        _classifyTicketHandler = classifyTicketHandler;
        _classificationHistoryHandler = classificationHistoryHandler;
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        try
        {
            var ticket = await _createTicketHandler.HandleAsync(new CreateTicketCommand(request.Title, request.Description), ct);
            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetAll(CancellationToken ct)
        => Ok(await _getAllTicketsHandler.HandleAsync(new GetAllTicketsQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDto>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _getTicketByIdHandler.HandleAsync(new GetTicketByIdQuery(id), ct));
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id:guid}/classify")]
    public async Task<IActionResult> Classify(Guid id, [FromQuery] string? provider, CancellationToken ct)
    {
        try
        {
            return Ok(await _classifyTicketHandler.HandleAsync(new ClassifyTicketCommand(id, provider), ct));
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ClassificationUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Klassifizierung derzeit nicht verfügbar.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}/classifications")]
    public async Task<IActionResult> GetClassificationHistory(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _classificationHistoryHandler.HandleAsync(new GetTicketClassificationHistoryQuery(id), ct));
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
