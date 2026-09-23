# bma-ai-ticket-system

Berufsmaturitätsarbeit (BMA), BMS Zürich, Klasse BIN23d.

**Titel:** Wie präzise kann ein selbst entwickeltes KI-Ticket-System die häufigsten IT-Probleme eines Lehrbetriebs klassifizieren und lösen?

**Autoren:** David Ilić, Finn Albrecht

## Thema

IT-Ticket-Systeme werden in Lehrbetrieben eingesetzt, um technische Probleme zu melden und zu lösen.
Diese Arbeit untersucht, wie präzise ein selbst entwickeltes KI-System die 10 häufigsten IT-Probleme
eines Lehrbetriebs klassifizieren und lösen kann – gemessen an Trefferquote, Lösungsqualität und
Lösungszeit, im Vergleich zum menschlichen IT-Support.

## Struktur

```
bma-ai-ticket-system/
  backend/    .NET-10-Web-API nach Domain-Driven Design (siehe backend/README.md)
  frontend/   React-Frontend (noch in Arbeit)
```

## Tech-Stack

C# (.NET 10) + React, KI-API (Anthropic Claude API oder OpenAI API).

## Stand

- **Backend:** erste funktionierende Ticket-Vertical-Slice (anlegen, auflisten, abrufen, klassifizieren)
  auf Basis einer regelbasierten Platzhalter-Klassifizierung. Details siehe [backend/README.md](backend/README.md).
- **Frontend:** noch nicht begonnen.
