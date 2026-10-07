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
- **Frontend:** React-Oberfläche mit den Ticketansichten „Deine“ und „Alle“, Erstelleranzeige und separatem Dashboard-Tab: Ticketzeiten einzeln, Anzahl gelöster Tickets, gesamte Lösungszeit und durchschnittliche Geschwindigkeit. Neue Tickets startet es automatisch mit OpenRouter.

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
