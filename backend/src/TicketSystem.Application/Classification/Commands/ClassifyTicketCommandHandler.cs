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
    private readonly IClassificationServiceResolver _classificationServiceResolver;

    public ClassifyTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketClassificationRepository classificationRepository,
        IClassificationServiceResolver classificationServiceResolver)
    {
        _ticketRepository = ticketRepository;
        _classificationRepository = classificationRepository;
        _classificationServiceResolver = classificationServiceResolver;
    }

    public async Task<ClassificationResultDto> HandleAsync(ClassifyTicketCommand command, CancellationToken ct = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, ct)
            ?? throw new TicketNotFoundException(command.TicketId);

        var classificationService = _classificationServiceResolver.Resolve(command.Provider);
        var outcome = await classificationService.ClassifyAsync(ticket.Title, ticket.Description, ct);

        var classification = new TicketClassification(
            ticket.Id,
            outcome.Category,
            new ClassificationConfidence(outcome.Confidence),
            outcome.SuggestedSolution,
            outcome.Source,
            outcome.Duration,
            outcome.Model,
            outcome.IsItRelated);

        await _classificationRepository.AddAsync(classification, ct);

        ticket.ApplyClassification(outcome.Category);
        await _ticketRepository.UpdateAsync(ticket, ct);

        return new ClassificationResultDto(
            ticket.Id,
            outcome.Category.ToString(),
            outcome.Confidence,
            outcome.SuggestedSolution,
            outcome.Source.ToString(),
            outcome.Duration,
            classification.CreatedAt,
            outcome.Model,
            outcome.IsItRelated,
            outcome.IsItRelated ? null : "Kein IT-Bezug, Ticket wurde nicht automatisch bearbeitet");
    }
}
