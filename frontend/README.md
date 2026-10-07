# TicketDesk Frontend

React- und TypeScript-Oberfläche für das Ticket-System. Das Frontend kann Tickets über die .NET-API
anzeigen und erstellen, KI-Klassifizierungen anfordern und gespeicherte Klassifizierungen abrufen.

## Voraussetzungen

- Node.js und npm
- Das Backend läuft unter `http://localhost:5278` (siehe [Backend-Anleitung](../backend/README.md))

## Starten

Im Backend zuerst die API starten. Danach in einem zweiten Terminal:

```powershell
npm install
npm run dev
```

Öffne die lokale URL, die Vite im Terminal ausgibt. Der Vite-Proxy leitet Anfragen unter `/api`
automatisch an `http://localhost:5278` weiter. API-Schlüssel gehören in die Backend-Konfiguration,
nicht in das Frontend.

## Prüfen

```powershell
npm run build
npm run lint
```
