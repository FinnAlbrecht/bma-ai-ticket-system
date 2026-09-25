import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'

type Ticket = { id: string; title: string; description: string; status: string; category: string; createdAt: string }
type ClassificationResult = { category: string; confidence: number; suggestedSolution: string }
const statusLabels: Record<string, string> = { Open: 'Offen', InProgress: 'In Bearbeitung', Resolved: 'Gelost' }

function App() {
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [filter, setFilter] = useState('Alle')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [classifying, setClassifying] = useState(false)
  const [notice, setNotice] = useState('')
  const [classification, setClassification] = useState<ClassificationResult | null>(null)

  const loadTickets = async () => {
    try { const response = await fetch('/api/tickets'); if (!response.ok) throw new Error('Tickets konnten nicht geladen werden.'); setTickets(await response.json()) }
    catch (error) { setNotice(error instanceof Error ? error.message : 'Verbindung zur API fehlgeschlagen.') }
    finally { setLoading(false) }
  }
  useEffect(() => { void loadTickets() }, [])
  const selectedTicket = tickets.find((ticket) => ticket.id === selectedId) ?? tickets[0]
  const visibleTickets = filter === 'Alle' ? tickets : tickets.filter((ticket) => statusLabels[ticket.status] === filter)

  const createTicket = async (event: FormEvent) => {
    event.preventDefault(); if (!title.trim() || !description.trim()) return; setSaving(true); setNotice('')
    try { const response = await fetch('/api/tickets', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ title, description }) }); if (!response.ok) throw new Error(await response.text()); const ticket: Ticket = await response.json(); setTickets((current) => [ticket, ...current]); setSelectedId(ticket.id); setTitle(''); setDescription(''); setNotice('Ticket wurde angelegt.') }
    catch (error) { setNotice(error instanceof Error ? error.message : 'Ticket konnte nicht angelegt werden.') }
    finally { setSaving(false) }
  }
  const classifyTicket = async () => {
    if (!selectedTicket) return; setClassifying(true); setNotice('')
    try { const response = await fetch(`/api/tickets/${selectedTicket.id}/classify`, { method: 'POST' }); if (!response.ok) throw new Error(await response.text()); setClassification(await response.json()); await loadTickets(); setNotice('Ticket wurde klassifiziert.') }
    catch (error) { setNotice(error instanceof Error ? error.message : 'Klassifizierung fehlgeschlagen.') }
    finally { setClassifying(false) }
  }

  return (
    <div className="app-shell"><header className="topbar"><a className="brand" href="/"><span className="brand-mark">TD</span><span><strong>TicketDesk</strong><small>Service Operations</small></span></a><div className="topbar-meta"><span className="live-dot" /> API verbunden <span className="separator" /> Workspace / Support</div></header><main><section className="page-heading"><div><p className="eyebrow">Support workspace</p><h1>Tickets im Blick behalten.</h1><p className="lede">Erfassen, priorisieren und mit KI-Klassifizierung schneller zur Lösung.</p></div><div className="heading-date"><span>HEUTE</span><strong>{new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date())}</strong></div></section><section className="metrics"><div className="metric"><span className="metric-label">Alle Tickets</span><strong>{tickets.length.toString().padStart(2, '0')}</strong><span className="metric-note">im Workspace</span></div><div className="metric"><span className="metric-label">Offen</span><strong>{tickets.filter((ticket) => ticket.status === 'Open').length.toString().padStart(2, '0')}</strong><span className="metric-note warm">brauchen Aufmerksamkeit</span></div><div className="metric"><span className="metric-label">Klassifiziert</span><strong>{tickets.filter((ticket) => ticket.category !== 'Unknown').length.toString().padStart(2, '0')}</strong><span className="metric-note">durch Assistenz</span></div><div className="metric accent-metric"><span className="metric-label">SLA heute</span><strong>94<span>%</span></strong><span className="metric-note">innerhalb der Zeit</span></div></section><div className="workspace-grid"><section className="panel ticket-panel"><div className="panel-head"><div><p className="eyebrow">Queue</p><h2>Aktuelle Tickets</h2></div><button className="icon-button" onClick={() => void loadTickets()} title="Tickets aktualisieren" aria-label="Tickets aktualisieren">↻</button></div><div className="filter-row">{['Alle', 'Offen', 'In Bearbeitung', 'Gelost'].map((option) => <button key={option} className={filter === option ? 'filter active' : 'filter'} onClick={() => setFilter(option)}>{option}<span>{option === 'Alle' ? tickets.length : tickets.filter((ticket) => statusLabels[ticket.status] === option).length}</span></button>)}</div><div className="ticket-list">{loading && <div className="empty-state">Tickets werden geladen ...</div>}{!loading && visibleTickets.length === 0 && <div className="empty-state">Keine Tickets in diesem Filter.</div>}{visibleTickets.map((ticket) => <button key={ticket.id} className={selectedTicket?.id === ticket.id ? 'ticket-row selected' : 'ticket-row'} onClick={() => { setSelectedId(ticket.id); setClassification(null) }}><span className="ticket-indicator" /><span className="ticket-copy"><strong>{ticket.title}</strong><small>{ticket.id.slice(0, 8).toUpperCase()} / {formatDate(ticket.createdAt)}</small></span><span className={`status status-${ticket.status.toLowerCase()}`}>{statusLabels[ticket.status] ?? ticket.status}</span><span className="row-chevron">›</span></button>)}</div></section><aside className="panel detail-panel">{selectedTicket ? <><div className="detail-top"><span className={`status status-${selectedTicket.status.toLowerCase()}`}>{statusLabels[selectedTicket.status] ?? selectedTicket.status}</span><span className="ticket-id">#{selectedTicket.id.slice(0, 8).toUpperCase()}</span></div><h2>{selectedTicket.title}</h2><p className="detail-description">{selectedTicket.description}</p><div className="detail-meta"><div><span>Erstellt</span><strong>{formatDate(selectedTicket.createdAt)}</strong></div><div><span>Kategorie</span><strong>{selectedTicket.category === 'Unknown' ? 'Noch offen' : selectedTicket.category}</strong></div></div><div className="assist-box"><div className="assist-title"><span className="spark">✦</span><strong>KI-Klassifizierung</strong><span className="assist-tag">BETA</span></div>{classification ? <><div className="classification-result"><strong>{classification.category}</strong><span>{Math.round(classification.confidence * 100)}% Konfidenz</span></div><p>{classification.suggestedSolution}</p></> : <p>Die Assistenz ordnet das Ticket einer Kategorie zu und schlägt einen nächsten Schritt vor.</p>}<button className="primary-button" onClick={() => void classifyTicket()} disabled={classifying}>{classifying ? 'Analysiere ...' : classification ? 'Erneut klassifizieren' : 'Ticket klassifizieren'} <span>→</span></button></div></> : <div className="empty-state detail-empty">Wähle ein Ticket aus.</div>}</aside></div><section className="create-section"><div className="create-intro"><p className="eyebrow">Neuer Vorgang</p><h2>Ticket erfassen</h2><p>Eine klare Beschreibung hilft der Assistenz bei der Einordnung.</p></div><form className="ticket-form" onSubmit={createTicket}><label><span>Titel</span><input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Worum geht es?" /></label><label><span>Beschreibung</span><textarea value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Beschreibe das Problem oder Anliegen ..." rows={3} /></label><button className="primary-button" disabled={saving || !title.trim() || !description.trim()}>{saving ? 'Wird angelegt ...' : 'Ticket anlegen'} <span>→</span></button></form></section>{notice && <div className="notice" role="status">{notice}</div>}</main><footer><span>TicketDesk / Support Operations</span><span>Built for clarity</span></footer></div>
  )
}

function formatDate(value: string) { return new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: 'short' }).format(new Date(value)) }

export default App
