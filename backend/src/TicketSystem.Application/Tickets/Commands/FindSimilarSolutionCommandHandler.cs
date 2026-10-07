using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TicketSystem.Application.Tickets.Dtos;
using TicketSystem.Domain.Tickets.Entities;
using TicketSystem.Domain.Tickets.Enums;
using TicketSystem.Domain.Tickets.Exceptions;
using TicketSystem.Domain.Tickets.Repositories;

namespace TicketSystem.Application.Tickets.Commands;

public sealed class FindSimilarSolutionCommandHandler(ITicketRepository ticketRepository)
{
    private const double MinimumSimilarity = 0.45;
    private static readonly HashSet<string> StopWords =
    [
        "aber", "auch", "auf", "aus", "bei", "bin", "bis", "bitte", "dass", "dem", "den", "der", "des",
        "die", "dies", "doch", "ein", "eine", "einer", "eines", "es", "für", "hat", "habe", "haben",
        "hier", "ich", "im", "in", "ist", "kein", "keine", "mit", "mein", "meine", "mir", "nach",
        "nicht", "noch", "oder", "problem", "seit", "sich", "sie", "sind", "und", "uns", "von", "vor",
        "war", "was", "weil", "wenn", "wie", "wieder", "wir", "wird", "wo", "zu"
    ];

    public async Task<SimilarSolutionDto> HandleAsync(
        FindSimilarSolutionCommand command,
        CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.TicketId, ct)
            ?? throw new TicketNotFoundException(command.TicketId);
        var tickets = await ticketRepository.GetAllAsync(ct);
        var requestedCategory = GuessCategory($"{ticket.Title} {ticket.Description}");
        var requestedTokens = Tokenize($"{ticket.Title} {ticket.Description}");
        var requestedTitleTokens = Tokenize(ticket.Title);

        var candidate = tickets
            .Where(other => other.Id != ticket.Id
                && other.Status is TicketStatus.Resolved or TicketStatus.Closed
                && !string.IsNullOrWhiteSpace(other.ResolutionNotes)
                && other.Category is not TicketCategory.Unclassified and not TicketCategory.OutOfScope
                && (requestedCategory is null || requestedCategory == other.Category))
            .Select(other => new
            {
                Ticket = other,
                Score = Similarity(
                    requestedTitleTokens,
                    requestedTokens,
                    Tokenize(other.Title),
                    Tokenize($"{other.Title} {other.Description}"))
            })
            .Where(candidate => candidate.Score >= MinimumSimilarity)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Ticket.ResolvedAt)
            .FirstOrDefault();

        if (candidate is null)
            return new SimilarSolutionDto(false, null, null, null, null, null, TicketDto.FromDomain(ticket));

        ticket.SuggestSimilarResolution(
            candidate.Ticket.Category,
            candidate.Ticket.ResolutionNotes!,
            candidate.Ticket.Id);
        await ticketRepository.UpdateAsync(ticket, ct);

        return new SimilarSolutionDto(
            true,
            candidate.Ticket.Id,
            candidate.Ticket.Title,
            candidate.Ticket.Category.ToString(),
            candidate.Ticket.ResolutionNotes,
            Math.Round(candidate.Score, 2),
            TicketDto.FromDomain(ticket));
    }

    private static double Similarity(
        HashSet<string> titleTokens,
        HashSet<string> allTokens,
        HashSet<string> candidateTitleTokens,
        HashSet<string> candidateAllTokens)
    {
        var titleSimilarity = Dice(titleTokens, candidateTitleTokens);
        var fullTextSimilarity = Dice(allTokens, candidateAllTokens);
        return titleSimilarity * 0.4 + fullTextSimilarity * 0.6;
    }

    private static double Dice(HashSet<string> left, HashSet<string> right)
    {
        if (left.Count == 0 || right.Count == 0) return 0;
        var overlap = left.Count(right.Contains);
        return 2d * overlap / (left.Count + right.Count);
    }

    private static HashSet<string> Tokenize(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var plainText = new string(normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray())
            .ToLowerInvariant();
        return Regex.Matches(plainText, @"[\p{L}\p{N}]{3,}")
            .Select(match => match.Value)
            .Where(token => !StopWords.Contains(token))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static TicketCategory? GuessCategory(string text)
    {
        var tokens = Tokenize(text);
        if (ContainsAny(tokens, "gesperrt", "sperre", "locked", "lockout")) return TicketCategory.AccountLockout;
        if (ContainsAny(tokens, "passwort", "password", "kennwort", "reset")) return TicketCategory.PasswordReset;
        if (ContainsAny(tokens, "wlan", "wifi", "wireless")) return TicketCategory.WifiConnectivity;
        if (ContainsAny(tokens, "drucker", "printer", "druckt")) return TicketCategory.PrinterIssue;
        if (ContainsAny(tokens, "absturz", "abgestuerzt", "crash", "stuerzt")) return TicketCategory.SoftwareCrash;
        if (ContainsAny(tokens, "tastatur", "keyboard", "maus", "mouse", "bildschirm", "monitor", "hardware")) return TicketCategory.HardwareDefect;
        if (ContainsAny(tokens, "internet", "netzwerk", "network", "ausfall")) return TicketCategory.NetworkOutage;
        if (ContainsAny(tokens, "email", "mail", "outlook")) return TicketCategory.EmailProblem;
        if (ContainsAny(tokens, "installation", "installieren", "install")) return TicketCategory.SoftwareInstallation;
        if (ContainsAny(tokens, "zugriff", "rechte", "berechtigung", "access")) return TicketCategory.AccessRights;
        return null;
    }

    private static bool ContainsAny(HashSet<string> tokens, params string[] candidates) =>
        candidates.Any(tokens.Contains);
}
