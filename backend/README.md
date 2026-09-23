# TicketSystem – Backend

.NET-9-Solution nach Domain-Driven-Design-Schichten. Erste funktionierende Ticket-Vertical-Slice:
Ticket anlegen, auflisten, abrufen und klassifizieren.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Starten

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
| GET     | `/api/tickets/{id}`            | Ein Ticket abrufen                     |
| POST    | `/api/tickets/{id}/classify`   | Ticket automatisch klassifizieren      |

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
      Ai/Clients/                  KeywordBasedClassificationService (Platzhalter für Claude/OpenAI API)
      Common/                      DI-Registrierung
```

## Aktueller Stand / nächste Schritte

- **Persistenz** ist bewusst In-Memory (`InMemoryTicketRepository`, `InMemoryTicketClassificationRepository`),
  damit ohne DB-Setup entwickelt und getestet werden kann. Da Application/Domain nur gegen die
  Repository-Interfaces programmieren, lässt sich das später durch EF Core (z. B. SQLite) ersetzen,
  ohne Domain/Application anzufassen.
- **Klassifizierung** läuft aktuell über `KeywordBasedClassificationService` (simple Stichwortsuche,
  kein echter KI-Call). Das ist ein Platzhalter für die in der Projektvereinbarung vorgesehene
  Anthropic-Claude- oder OpenAI-API-Anbindung. Da `IClassificationService` in der Application-Schicht
  definiert ist, betrifft der Umstieg auf die echte KI-API nur eine neue Implementierung in
  `TicketSystem.Infrastructure/Ai/Clients/` plus die DI-Registrierung in `InfrastructureServiceCollectionExtensions`.
- Die 10 Ticket-Kategorien in `TicketCategory` sind ein erster Vorschlag basierend auf den Beispielen aus
  der Projektvereinbarung (Passwort-Reset, WLAN, Drucker, ...) – ggf. an die real erhobenen 10 Ticket-Typen
  aus dem Lehrbetrieb anpassen.
- **Tests fehlen noch komplett** (kein Testprojekt). Als Nächstes: Unit-Tests für die Domain-Regeln
  (`Ticket`, `TicketClassification`) und Integrationstests für die Endpoints.
- Für den in der Projektvereinbarung vorgesehenen Vergleich KI vs. menschlicher Support fehlt aktuell
  ein Weg, eine menschliche Klassifizierung/Lösung zum selben Ticket zu erfassen (`ClassificationSource.Human`
  ist im Enum vorbereitet, wird aber noch nirgends genutzt).
