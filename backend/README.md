# TicketSystem – Backend

.NET-10-Solution nach Domain-Driven-Design-Schichten. Aktuell nur ein Test-Setup
mit lauffähiger Web API + Swagger; die Domain-/Application-/Infrastructure-Ordner
sind bewusst leer (`.gitkeep`) und werden mit dem eigentlichen Ticket-Domänenmodell
befüllt.

## Starten

```bash
dotnet run --project src/TicketSystem.Api
```

Swagger UI: `http://localhost:<port>/swagger`
Test-Endpoint: `GET /api/test`

## Struktur

```
backend/
  TicketSystem.sln
  src/
    TicketSystem.Api/              Presentation-Layer: Controller, Program.cs, Swagger
    TicketSystem.Domain/           Entities, ValueObjects, Enums, Events, Repository-Interfaces (pro Bounded Context)
      Tickets/
      Classification/
      Common/
    TicketSystem.Application/      Use Cases (Commands/Queries), DTOs, Interfaces
      Tickets/
      Classification/
      Common/
    TicketSystem.Infrastructure/   Persistence, KI-API-Anbindung
      Persistence/
      Ai/
      Common/
```
