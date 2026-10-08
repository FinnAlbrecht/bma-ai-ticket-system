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
  frontend/   React-Frontend für Ticketbearbeitung und KI-Klassifizierung
```

## Tech-Stack

C# (.NET 10) + React, KI-API (OpenRouter oder Anthropic Claude).

## Stand

- **Backend:** SQLite speichert Konten, Tickets und KI-Klassifizierungen dauerhaft. Teammitglieder melden sich mit E-Mail und Passwort an; Tickets werden ihrem Ersteller zugeordnet. Ähnliche gelöste Tickets können ihre Lösung wiederverwenden und als hilfreich bestätigt werden. Details siehe [backend/README.md](backend/README.md).
- **Frontend:** React-Oberfläche mit den Ticketansichten „Deine“ und „Alle“, Erstelleranzeige, einem dauerhaft gespeicherten KI-Chat pro eigenem Ticket und einem separaten Dashboard-Tab. Nur der Ticket-Ersteller kann den Chat, erneute OpenRouter-Anfragen und Ticket-Aktionen verwenden; Teammitglieder können Tickets lesend ansehen.

## Lokal starten

Backend und Frontend benötigen je ein eigenes Terminal. Die .NET-API muss auf Port `5278` laufen;
der Vite-Entwicklungsserver leitet `/api`-Anfragen an diese API weiter.

1. Backend gemäss [backend/README.md](backend/README.md) starten.
2. Im zweiten Terminal das Frontend starten:

   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

3. Die im Terminal angezeigte lokale Vite-Adresse im Browser öffnen.

Der API-Schlüssel wird ausschliesslich im Backend konfiguriert. Für die Klassifizierung mit OpenRouter
oder Claude muss der jeweilige Schlüssel dort gesetzt sein; der Keyword-Provider benötigt keinen
externen KI-Schlüssel.

## Kostenlos online bereitstellen

Das Repository enthält eine Render-Konfiguration (`render.yaml`) und ein Dockerfile. Für dauerhaft
gespeicherte Tickets wird eine PostgreSQL-Datenbank bei Neon verwendet; die Anwendung kann lokal
weiterhin SQLite nutzen.

1. Erstelle ein kostenloses PostgreSQL-Projekt bei [Neon](https://neon.tech/) und kopiere dessen
   PostgreSQL-Verbindungs-URL.
2. Verbinde dieses GitHub-Repository in [Render](https://render.com/) über **New → Blueprint**.
   Render liest `render.yaml` und baut Frontend und Backend gemeinsam.
3. Hinterlege im Render-Service `ConnectionStrings__TicketDatabase` mit der Neon-Verbindungs-URL,
   `OpenRouter__ApiKey` mit deinem OpenRouter-Schlüssel und `DeleteAll__Password` mit einem langen,
   eigenen Löschpasswort. Niemals Schlüssel oder Passwörter ins Repository oder in den Chat schreiben.
4. Nach erfolgreichem Build ist die von Render angezeigte URL die öffentliche Anwendung. Der erste
   Aufruf kann auf dem kostenlosen Tarif verzögert starten; kostenlose Tarife und Limits können sich
   ändern.

Die gehostete Datenbank startet leer; lokale SQLite-Tickets werden nicht automatisch übertragen.
Die Registrierung ist öffentlich und angemeldete Nutzer können Team-Tickets sehen und löschen.
Verwende deshalb keine vertraulichen oder personenbezogenen Ticketinhalte und teile die URL nur,
wenn dieser Zugriff für dein Team in Ordnung ist.
