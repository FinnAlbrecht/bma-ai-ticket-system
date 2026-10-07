using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TicketSystem.Api.Contracts;
using TicketSystem.Application.Classification.Commands;
using TicketSystem.Application.Classification.Queries;
using TicketSystem.Application.Common.Exceptions;
using TicketSystem.Application.Tickets.Commands;
using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Application.Tickets.Queries;
using TicketSystem.Domain.Tickets.Exceptions;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly CreateTicketCommandHandler _createTicketHandler;
    private readonly GetAllTicketsQueryHandler _getAllTicketsHandler;
    private readonly GetTicketByIdQueryHandler _getTicketByIdHandler;
    private readonly ClassifyTicketCommandHandler _classifyTicketHandler;
    private readonly GetTicketClassificationHistoryQueryHandler _classificationHistoryHandler;
    private readonly FindSimilarSolutionCommandHandler _findSimilarSolutionHandler;
    private readonly AcceptSuggestedSolutionCommandHandler _acceptSuggestedSolutionHandler;
    private readonly ResolveOutOfScopeTicketCommandHandler _resolveOutOfScopeTicketHandler;
    private readonly ITicketRepository _ticketRepository;

    public TicketsController(
        CreateTicketCommandHandler createTicketHandler,
        GetAllTicketsQueryHandler getAllTicketsHandler,
        GetTicketByIdQueryHandler getTicketByIdHandler,
        ClassifyTicketCommandHandler classifyTicketHandler,
        GetTicketClassificationHistoryQueryHandler classificationHistoryHandler,
        FindSimilarSolutionCommandHandler findSimilarSolutionHandler,
        AcceptSuggestedSolutionCommandHandler acceptSuggestedSolutionHandler,
        ResolveOutOfScopeTicketCommandHandler resolveOutOfScopeTicketHandler,
        ITicketRepository ticketRepository)
    {
        _createTicketHandler = createTicketHandler;
        _getAllTicketsHandler = getAllTicketsHandler;
        _getTicketByIdHandler = getTicketByIdHandler;
        _classifyTicketHandler = classifyTicketHandler;
        _classificationHistoryHandler = classificationHistoryHandler;
        _findSimilarSolutionHandler = findSimilarSolutionHandler;
        _acceptSuggestedSolutionHandler = acceptSuggestedSolutionHandler;
        _resolveOutOfScopeTicketHandler = resolveOutOfScopeTicketHandler;
        _ticketRepository = ticketRepository;
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        try
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var displayName = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(displayName))
                return Unauthorized();

            var ticket = await _createTicketHandler.HandleAsync(
                new CreateTicketCommand(request.Title, request.Description, userId, displayName),
                ct);
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

    [HttpDelete]
    public async Task<IActionResult> DeleteAll(CancellationToken ct)
    {
        await _ticketRepository.DeleteAllAsync(ct);
        return NoContent();
    }

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
        catch (TicketAlreadyClosedException ex)
        {
            return Conflict(ex.Message);
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

    [HttpPost("{id:guid}/similar-solution")]
    public async Task<IActionResult> FindSimilarSolution(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _findSimilarSolutionHandler.HandleAsync(new FindSimilarSolutionCommand(id), ct));
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (TicketAlreadyClosedException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:guid}/accept-suggested-solution")]
    public async Task<IActionResult> AcceptSuggestedSolution(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _acceptSuggestedSolutionHandler.HandleAsync(new AcceptSuggestedSolutionCommand(id), ct));
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (TicketAlreadyClosedException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/resolve-out-of-scope")]
    public async Task<ActionResult<TicketDto>> ResolveOutOfScope(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _resolveOutOfScopeTicketHandler.HandleAsync(
                new ResolveOutOfScopeTicketCommand(id), ct));
        }
        catch (TicketNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (TicketAlreadyClosedException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
