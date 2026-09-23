using TicketSystem.Application.Classification.Dtos;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Classification.Entities;
using TicketSystem.Domain.Classification.Enums;
using TicketSystem.Domain.Classification.Repositories;
using TicketSystem.Domain.Classification.ValueObjects;
using TicketSystem.Domain.Tickets.Exceptions;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Classification.Commands;

public class ClassifyTicketCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketClassificationRepository _classificationRepository;
    private readonly IClassificationService _classificationService;

    public ClassifyTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketClassificationRepository classificationRepository,
        IClassificationService classificationService)
    {
        _ticketRepository = ticketRepository;
        _classificationRepository = classificationRepository;
        _classificationService = classificationService;
    }

    public async Task<ClassificationResultDto> HandleAsync(ClassifyTicketCommand command, CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, ct)
            ?? throw new TicketNotFoundException(command.TicketId);

        var outcome = await _classificationService.ClassifyAsync(ticket.Title, ticket.Description, ct);

        var classification = new TicketClassification(
            ticket.Id,
            outcome.Category,
            new ClassificationConfidence(outcome.Confidence),
            outcome.SuggestedSolution,
            ClassificationSource.Ai,
            outcome.Duration);

        await _classificationRepository.AddAsync(classification, ct);

        ticket.ApplyClassification(outcome.Category);
        await _ticketRepository.UpdateAsync(ticket, ct);

        return new ClassificationResultDto(
            ticket.Id,
            outcome.Category.ToString(),
            outcome.Confidence,
            outcome.SuggestedSolution,
            ClassificationSource.Ai.ToString(),
            outcome.Duration,
            classification.CreatedAt);
    }
}
