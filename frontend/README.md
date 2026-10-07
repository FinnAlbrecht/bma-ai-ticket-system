# TicketDesk Frontend

React- und TypeScript-Oberfläche für das Ticket-System. Das Frontend kann Tickets über die .NET-API
anzeigen und erstellen. Nach dem Erstellen startet automatisch eine Klassifizierung über OpenRouter;
Nach dem Erstellen wird zuerst nach einer passenden, bereits gelösten Anfrage gesucht. Eine gefundene
Lösung muss der Nutzer als hilfreich bestätigen; andernfalls kann OpenRouter erneut gefragt werden.
Ohne ähnlichen Treffer startet OpenRouter automatisch. Das Backend speichert Tickets und Lösungen in
SQLite. Die Ticket-Detailseite zeigt Klassifizierung, Lösungsvorschlag sowie KI- und gesamte
Lösungszeit. Der Dashboard-Tab im oberen Menü listet pro Ticket die KI- und Lösungszeit und fasst
gelöste Tickets, Gesamtzeiten und durchschnittliche Geschwindigkeit zusammen. Tickets ohne
IT-Bezug erscheinen nicht im Filter „Offen“, sondern in einem eigenen Filter. Nutzer können sie
als gelöst markieren; danach zeigen sie im Gelöst-Filter den zusätzlichen Hinweis, dass sie nicht
technisch gelöst wurden.

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

Beim ersten Öffnen erstellst du ein Konto mit Anzeigename, E-Mail und Passwort. Danach kannst du
dich anmelden. Die Ticketübersicht hat die Ansichten „Deine“ (von deinem Konto erstellt) und „Alle“;
in „Alle“ wird der jeweilige Ersteller angezeigt. Konten und Tickets teilen dieselbe Backend-
Datenbank, sodass Teammitglieder dieselbe Ticketliste verwenden.

Für die automatische Klassifizierung muss der OpenRouter-API-Schlüssel im Backend gesetzt sein.
Ohne gültigen Schlüssel wird das Ticket zwar erstellt, aber die Oberfläche meldet den Fehler bei der
Klassifizierung; es erfolgt kein stiller Wechsel zu einem anderen Provider.

## Prüfen

```powershell
npm run build
npm run lint
```
