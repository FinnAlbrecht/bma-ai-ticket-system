# TicketSystem – Backend

.NET-10-Solution nach Domain-Driven-Design-Schichten. Erste funktionierende Ticket-Vertical-Slice:
Ticket anlegen, auflisten, abrufen und klassifizieren.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Starten

OpenRouter ist der Standard-Provider. Den API-Key lokal als User-Secret setzen:

```bash
dotnet user-secrets set "OpenRouter:ApiKey" "<dein-openrouter-api-key>" --project src/TicketSystem.Api
```

Standardmodell ist `nvidia/nemotron-3-ultra-550b-a55b:free`. Wenn der API-Key bereits geteilt oder
veröffentlicht wurde, widerrufe ihn zuerst und verwende nur einen neu erstellten Key. Das Modell
kann bei OpenRouter geändert werden; prüfe dort den aktuellen Gratis-Status, verfügbare Credits und
Nutzungslimits. Das freie Modell kann auf OpenRouter mit der `:free`-Suffix-Variante angegeben werden,
beispielsweise `nvidia/nemotron-3-ultra-550b-a55b:free`.

Alternativ kann die Umgebungsvariable `OpenRouter__ApiKey` gesetzt werden. Für Claude:
`Anthropic:ApiKey` beziehungsweise `Anthropic__ApiKey`. API-Keys gehören nicht in versionierte
Dateien. Ohne gültigen Key startet die Anwendung mit einer Warnung; Klassifizierungsaufrufe liefern
HTTP 503 und wechseln nicht unbemerkt auf Stichwortregeln.

```bash
dotnet run --project src/TicketSystem.Api
```

Swagger UI: `http://localhost:<port>/swagger`

## Endpoints

| Methode | Route                       | Beschreibung                          |
|---------|------------------------------|----------------------------------------|
| GET     | `/api/test`                  | Health-Check                           |
| POST    | `/api/tickets`                | Neues Ticket anlegen (`title`, `description`) |
| GET     | `/api/tickets`                | Alle Tickets auflisten                 |
| DELETE  | `/api/tickets`                | Alle Tickets samt Klassifizierungen löschen |
| GET     | `/api/tickets/{id}`            | Ein Ticket abrufen                     |
| POST    | `/api/tickets/{id}/classify`   | Ticket automatisch klassifizieren      |
| GET     | `/api/tickets/{id}/classifications` | Klassifizierungshistorie abrufen   |
| GET     | `/api/dashboard/metrics` | Ticketstatus sowie KI- und Lösungszeiten zusammenfassen |
| POST    | `/api/tickets/{id}/similar-solution` | Ähnliche gelöste Tickets suchen und Lösung vorschlagen |
| POST    | `/api/tickets/{id}/accept-suggested-solution` | Vorgeschlagene Lösung als hilfreich bestätigen |
| POST    | `/api/tickets/{id}/resolve-out-of-scope` | Ticket ohne IT-Bezug als gelöst markieren |

Der Standard-Provider lässt sich in `appsettings.json` über `Classification:Provider` auf
`OpenRouter`, `Claude` oder `Keyword` setzen. Für einen direkten Vergleich kann derselbe Endpunkt
mit `?provider=openrouter`, `?provider=claude` oder `?provider=keyword` aufgerufen werden; jede Klassifizierung wird getrennt mit Kategorie,
Lösungsvorschlag, Confidence, Dauer, Quelle und Modell gespeichert.

OpenRouter und Claude klassifizieren ausschliesslich IT-bezogene Tickets. Bei fehlendem IT-Bezug wird kein
Lösungsvorschlag erzeugt; die Historie weist das Ergebnis als `OutOfScope` aus.
Während der Klassifizierung steht ein Ticket auf `InProgress`. Wird eine passende Lösung aus einem
früheren Ticket gefunden, erhält es den Status `AnswerFound`, bis der Nutzer die Lösung bestätigt
oder OpenRouter erneut fragt. Bei einer IT-bezogenen KI-Antwort mit
Lösungsvorschlag wird es automatisch auf `Resolved` gesetzt; Tickets ohne IT-Bezug bleiben klassifiziert,
aber ungelöst. Der Dashboard-Endpunkt liefert je Ticket die Dauer der letzten KI-Klassifizierung und
fasst gelöste Tickets sowie KI- und Lösungszeiten (Durchschnitt und Summe) zusammen.
OpenRouter-Modell und -Timeout lassen sich unter `OpenRouter:Model` und
`OpenRouter:TimeoutSeconds` konfigurieren. API-Aufrufe können Kosten verursachen; setze beim
jeweiligen Provider ein Ausgabenlimit.

## Teamkonten

Teammitglieder registrieren sich in der Oberfläche mit Anzeigename, E-Mail-Adresse und einem
Passwort mit mindestens 10 Zeichen. Passwörter werden gehasht gespeichert; die Anmeldung läuft
über ein HttpOnly-Sitzungscookie. Ticket- und Dashboard-Endpunkte sind nur angemeldet erreichbar.
Neue Tickets werden dem angemeldeten Konto zugeordnet, damit die Oberfläche zwischen „Deine“ und
„Alle“ Tickets filtern und den Ersteller anzeigen kann. Bisherige Tickets bleiben erhalten; da sie
vor der Anmeldung erstellt wurden, ist ihr Ersteller unbekannt und sie erscheinen nur unter „Alle“.

## Struktur

```
backend/
  TicketSystem.sln
  src/
    TicketSystem.Api/              Presentation-Layer: Controller, Program.cs, Swagger
    TicketSystem.Domain/           Entities, ValueObjects, Enums, Repository-Interfaces (pro Bounded Context)
      Tickets/                     Ticket-Aggregat (Entity, Enums, Repository-Interface, Exceptions)
      Classification/              Klassifizierungshistorie (Entity, Enums, ValueObjects, Repository-Interface)
      Common/
    TicketSystem.Application/      Use Cases (Commands/Queries), DTOs, Ports
      Tickets/                     CreateTicket, GetAllTickets, GetTicketById
      Classification/              ClassifyTicket
      Common/                      IClassificationService (Port), DI-Registrierung
    TicketSystem.Infrastructure/   Adapter: Persistence, KI-Anbindung
      Persistence/Repositories/    In-Memory-Repositories (Platzhalter für EF Core/SQLite)
      Ai/Clients/                  OpenRouterClassificationService, ClaudeClassificationService,
                                   KeywordBasedClassificationService
      Common/                      DI-Registrierung
```

## Aktueller Stand / nächste Schritte

- **Persistenz** nutzt SQLite in `src/TicketSystem.Api/tickets.db`. Tickets und KI-Klassifizierungen
  bleiben dadurch über Backend-Neustarts hinweg gespeichert. Die Tabellen werden beim Start automatisch
  angelegt; ein eigener Datenbankserver ist nicht nötig. Der Pfad kann über
  `ConnectionStrings__TicketDatabase` überschrieben werden.
- Für Deployments kann `Database__Provider=PostgreSQL` und
  `ConnectionStrings__TicketDatabase` gesetzt werden. Lokal bleibt SQLite der Standard.
- Bei neuen Tickets sucht das Backend zuerst nach einer gelösten Anfrage mit passender Kategorie und
  ausreichender Textähnlichkeit. Eine gefundene Lösung wird erst nach „Hat geholfen“ als gelöst markiert.
  Andernfalls kann der Nutzer OpenRouter erneut fragen; ohne passenden Treffer startet OpenRouter
  automatisch wie bisher.
- **Klassifizierung** nutzt standardmässig OpenRouter, optional Claude oder ausdrücklich gewählte
  lokale Stichwortregeln. KI-Ausfälle führen nicht zu einem stillen Wechsel auf die Stichwortsuche.
  Das offizielle `Anthropic` NuGet-Paket wird ausschliesslich in Infrastructure verwendet.
- Die zehn fachlichen IT-Kategorien in `TicketCategory` sind ein erster Vorschlag basierend auf den Beispielen aus
  der Projektvereinbarung (Passwort-Reset, WLAN, Drucker, ...); `Other` und `OutOfScope` ergänzen diese für
  unbekannte bzw. nicht IT-bezogene Tickets.
- Tests befinden sich im Projekt `tests/TicketSystem.Tests`.
- Für den in der Projektvereinbarung vorgesehenen Vergleich KI vs. menschlicher Support fehlt aktuell
  ein Weg, eine menschliche Klassifizierung/Lösung zum selben Ticket zu erfassen (`ClassificationSource.Human`
  ist im Enum vorbereitet, wird aber noch nirgends genutzt).
