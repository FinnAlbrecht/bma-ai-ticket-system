using System.Diagnostics;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Tickets.Enums;

namespace TicketSystem.Infrastructure.Ai.Clients;

/// <summary>
/// Platzhalter-Implementierung von IClassificationService: ordnet Tickets rein anhand von
/// Stichwörtern einer Kategorie zu. Muss durch einen echten Aufruf der Anthropic Claude API
/// oder OpenAI API ersetzt werden (siehe Projektvereinbarung 2.4 "Hilfsmittel"). Da
/// IClassificationService in der Application-Schicht definiert ist, betrifft der Austausch
/// nur diese Klasse und die DI-Registrierung in AddInfrastructure.
/// </summary>
public class KeywordBasedClassificationService : IClassificationService
{
    private static readonly (TicketCategory Category, string[] Keywords, string Solution)[] Rules =
    [
        (TicketCategory.PasswordReset, ["passwort", "password", "login"], "Passwort über den Self-Service-Reset zurücksetzen lassen."),
        (TicketCategory.WifiConnectivity, ["wlan", "wifi", "wi-fi"], "WLAN-Adapter neu verbinden, ggf. Router neu starten."),
        (TicketCategory.PrinterIssue, ["drucker", "printer", "druck"], "Druckertreiber neu installieren und Druckwarteschlange leeren."),
        (TicketCategory.SoftwareCrash, ["absturz", "crash", "hängt"], "Anwendung neu starten, ggf. auf die neueste Version aktualisieren."),
        (TicketCategory.HardwareDefect, ["hardware", "defekt", "kaputt"], "Gerät durch den IT-Support prüfen und ggf. austauschen lassen."),
        (TicketCategory.NetworkOutage, ["internet", "netzwerk", "verbindung"], "Netzwerkstatus prüfen, Kabel/Switch kontrollieren."),
        (TicketCategory.EmailProblem, ["email", "e-mail", "outlook", "mail"], "E-Mail-Konto neu synchronisieren, Postfachgröße prüfen."),
        (TicketCategory.SoftwareInstallation, ["installation", "installieren"], "Installation über das Software-Center anfordern."),
        (TicketCategory.AccessRights, ["zugriff", "berechtigung", "rechte"], "Berechtigung beim zuständigen Datenverantwortlichen anfragen."),
        (TicketCategory.AccountLockout, ["gesperrt", "account", "konto"], "Konto durch den IT-Support entsperren lassen."),
    ];

    public Task<ClassificationOutcome> ClassifyAsync(string title, string description, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var text = $"{title} {description}".ToLowerInvariant();

        foreach (var rule in Rules)
        {
            if (rule.Keywords.Any(text.Contains))
            {
                stopwatch.Stop();
                return Task.FromResult(new ClassificationOutcome(rule.Category, 0.85, rule.Solution, stopwatch.Elapsed));
            }
        }

        stopwatch.Stop();
        return Task.FromResult(new ClassificationOutcome(
            TicketCategory.Other,
            0.3,
            "Keine automatische Lösung gefunden – bitte manuell durch den IT-Support prüfen.",
            stopwatch.Elapsed));
    }
}
