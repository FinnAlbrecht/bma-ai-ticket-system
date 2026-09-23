using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Contracts;
using TicketSystem.Application.Classification.Commands;
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

    public TicketsController(
        CreateTicketCommandHandler createTicketHandler,
        GetAllTicketsQueryHandler getAllTicketsHandler,
        GetTicketByIdQueryHandler getTicketByIdHandler,
        ClassifyTicketCommandHandler classifyTicketHandler)
    {
        _createTicketHandler = createTicketHandler;
        _getAllTicketsHandler = getAllTicketsHandler;
        _getTicketByIdHandler = getTicketByIdHandler;
        _classifyTicketHandler = classifyTicketHandler;
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
    public async Task<IActionResult> Classify(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _classifyTicketHandler.HandleAsync(new ClassifyTicketCommand(id), ct));
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
