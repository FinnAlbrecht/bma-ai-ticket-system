import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import {
  classifyTicket as classifyTicketRequest,
  createTicket as createTicketRequest,
  getApiHealth,
  getClassificationHistory,
  getTickets,
} from './api'
import type { ClassificationResult, Provider, Ticket, TicketClassification } from './api'
import './App.css'

type ApiStatus = 'checking' | 'connected' | 'disconnected'
type TicketFilter = 'Alle' | 'Offen' | 'In Bearbeitung' | 'Gelöst'

const statusLabels: Record<string, string> = {
  New: 'Offen',
  Classified: 'Klassifiziert',
  InProgress: 'In Bearbeitung',
  Resolved: 'Gelöst',
  Closed: 'Geschlossen',
}

const categoryLabels: Record<string, string> = {
  Unclassified: 'Noch offen',
  PasswordReset: 'Passwort zurücksetzen',
  WifiConnectivity: 'WLAN-Verbindung',
  PrinterIssue: 'Druckerproblem',
  SoftwareCrash: 'Softwarefehler',
  HardwareDefect: 'Hardwaredefekt',
  NetworkOutage: 'Netzwerkausfall',
  EmailProblem: 'E-Mail-Problem',
  SoftwareInstallation: 'Softwareinstallation',
  AccessRights: 'Zugriffsrechte',
  AccountLockout: 'Konto gesperrt',
  Other: 'Sonstiges',
  OutOfScope: 'Kein IT-Bezug',
}

const filters: TicketFilter[] = ['Alle', 'Offen', 'In Bearbeitung', 'Gelöst']

function isOpenStatus(status: string) {
  return status === 'New' || status === 'Classified'
}

function isProvider(value: string): value is Provider {
  return value === 'OpenRouter' || value === 'Claude' || value === 'Keyword'
}

function App() {
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [filter, setFilter] = useState<TicketFilter>('Alle')
  const [provider, setProvider] = useState<Provider>('OpenRouter')
  const [apiStatus, setApiStatus] = useState<ApiStatus>('checking')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [classifying, setClassifying] = useState(false)
  const [notice, setNotice] = useState('')
  const [classification, setClassification] = useState<ClassificationResult | null>(null)
  const [history, setHistory] = useState<TicketClassification[]>([])
  const [historyTicketId, setHistoryTicketId] = useState<string | null>(null)

  const loadTickets = async () => {
    try {
      const loadedTickets = await getTickets()
      setTickets(loadedTickets)
      setApiStatus('connected')
    } catch (error) {
      setApiStatus('disconnected')
      setNotice(error instanceof Error ? error.message : 'Verbindung zur API fehlgeschlagen.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let active = true

    async function initialize() {
      try {
        const loadedTickets = await getTickets()
        if (active) {
          setTickets(loadedTickets)
          setApiStatus('connected')
        }
      } catch (error) {
        if (active) {
          setApiStatus('disconnected')
          setNotice(error instanceof Error ? error.message : 'Verbindung zur API fehlgeschlagen.')
        }
      } finally {
        if (active) setLoading(false)
      }

      try {
        await getApiHealth()
        if (active) setApiStatus('connected')
      } catch {
        if (active) setApiStatus('disconnected')
      }
    }

    void initialize()
    return () => { active = false }
  }, [])

  const selectedTicket = tickets.find((ticket) => ticket.id === selectedId) ?? tickets[0]
  const selectedTicketId = selectedTicket?.id
  const selectedHistory = historyTicketId === selectedTicketId ? history : []
  const historyLoading = Boolean(selectedTicketId && historyTicketId !== selectedTicketId)
  const visibleTickets = tickets.filter((ticket) => {
    if (filter === 'Alle') return true
    if (filter === 'Offen') return isOpenStatus(ticket.status)
    if (filter === 'In Bearbeitung') return ticket.status === 'InProgress'
    return ticket.status === 'Resolved' || ticket.status === 'Closed'
  })
  const classifiedCount = tickets.filter((ticket) => ticket.category !== 'Unclassified').length
  const openCount = tickets.filter((ticket) => isOpenStatus(ticket.status)).length

  useEffect(() => {
    if (!selectedTicketId) return

    let active = true

    void getClassificationHistory(selectedTicketId)
      .then((items) => {
        if (active) {
          setHistory(items)
          setHistoryTicketId(selectedTicketId)
        }
      })
      .catch((error: unknown) => {
        if (active) {
          setHistory([])
          setHistoryTicketId(selectedTicketId)
          setNotice(error instanceof Error ? error.message : 'Klassifizierungshistorie konnte nicht geladen werden.')
        }
      })

    return () => { active = false }
  }, [selectedTicketId])

  const createTicket = async (event: FormEvent) => {
    event.preventDefault()
    const cleanTitle = title.trim()
    const cleanDescription = description.trim()
    if (!cleanTitle || !cleanDescription) return
    setSaving(true)
    setNotice('')

    try {
      const ticket = await createTicketRequest(cleanTitle, cleanDescription)
      setTickets((current) => [ticket, ...current])
      setSelectedId(ticket.id)
      setClassification(null)
      setTitle('')
      setDescription('')
      setApiStatus('connected')
      setNotice('Ticket wurde angelegt.')
    } catch (error) {
      setApiStatus('disconnected')
      setNotice(error instanceof Error ? error.message : 'Ticket konnte nicht angelegt werden.')
    } finally {
      setSaving(false)
    }
  }

  const runClassification = async () => {
    if (!selectedTicket) return
    setClassifying(true)
    setNotice('')

    try {
      const result = await classifyTicketRequest(selectedTicket.id, provider)
      setClassification(result)
      setHistory((current) => [{
        category: result.category,
        confidence: result.confidence,
        suggestedSolution: result.suggestedSolution,
        source: result.source,
        model: result.model,
        isItRelated: result.isItRelated,
        createdAt: result.createdAt,
      }, ...current])
      setApiStatus('connected')
      await loadTickets()
      setNotice(result.isItRelated
        ? `Ticket wurde mit ${provider} klassifiziert.`
        : result.message ?? 'Das Ticket hat keinen IT-Bezug.')
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Klassifizierung fehlgeschlagen.')
    } finally {
      setClassifying(false)
    }
  }

  const refreshTickets = () => {
    setNotice('')
    void loadTickets()
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="/">
          <span className="brand-mark">TD</span>
          <span><strong>TicketDesk</strong><small>Service Operations</small></span>
        </a>
        <div className="topbar-meta">
          <span className={`live-dot ${apiStatus === 'disconnected' ? 'offline' : ''}`} />
          {apiStatus === 'checking' ? 'API wird geprüft' : apiStatus === 'connected' ? 'API verbunden' : 'API nicht erreichbar'}
          <span className="separator" /> Workspace / Support
        </div>
      </header>
      <main>
        <section className="page-heading">
          <div>
            <p className="eyebrow">Support workspace</p>
            <h1>Tickets im Blick behalten.</h1>
            <p className="lede">Erfassen, priorisieren und mit KI-Klassifizierung schneller zur Lösung.</p>
          </div>
          <div className="heading-date">
            <span>HEUTE</span>
            <strong>{new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date())}</strong>
          </div>
        </section>

        <section className="metrics">
          <div className="metric"><span className="metric-label">Alle Tickets</span><strong>{tickets.length.toString().padStart(2, '0')}</strong><span className="metric-note">im Workspace</span></div>
          <div className="metric"><span className="metric-label">Offen</span><strong>{openCount.toString().padStart(2, '0')}</strong><span className="metric-note warm">brauchen Aufmerksamkeit</span></div>
          <div className="metric"><span className="metric-label">Klassifiziert</span><strong>{classifiedCount.toString().padStart(2, '0')}</strong><span className="metric-note">durch Assistenz</span></div>
          <div className="metric accent-metric"><span className="metric-label">API-Status</span><strong className="api-metric">{apiStatus === 'connected' ? 'OK' : apiStatus === 'checking' ? '…' : 'Offline'}</strong><span className="metric-note">Backend-Verbindung</span></div>
        </section>

        <div className="workspace-grid">
          <section className="panel ticket-panel">
            <div className="panel-head">
              <div><p className="eyebrow">Queue</p><h2>Aktuelle Tickets</h2></div>
              <button className="icon-button" onClick={refreshTickets} title="Tickets aktualisieren" aria-label="Tickets aktualisieren">↻</button>
            </div>
            <div className="filter-row">
              {filters.map((option) => (
                <button key={option} className={filter === option ? 'filter active' : 'filter'} onClick={() => setFilter(option)}>
                  {option}<span>{option === 'Alle' ? tickets.length : tickets.filter((ticket) => {
                    if (option === 'Offen') return isOpenStatus(ticket.status)
                    if (option === 'In Bearbeitung') return ticket.status === 'InProgress'
                    return ticket.status === 'Resolved' || ticket.status === 'Closed'
                  }).length}</span>
                </button>
              ))}
            </div>
            <div className="ticket-list">
              {loading && <div className="empty-state">Tickets werden geladen ...</div>}
              {!loading && visibleTickets.length === 0 && <div className="empty-state">{apiStatus === 'disconnected' ? 'Die API ist nicht erreichbar. Starte das Backend und aktualisiere die Liste.' : 'Keine Tickets in diesem Filter.'}</div>}
              {visibleTickets.map((ticket) => (
                <button
                  key={ticket.id}
                  className={selectedTicket?.id === ticket.id ? 'ticket-row selected' : 'ticket-row'}
                  onClick={() => { setSelectedId(ticket.id); setClassification(null); setNotice('') }}
                >
                  <span className="ticket-indicator" />
                  <span className="ticket-copy"><strong>{ticket.title}</strong><small>{ticket.id.slice(0, 8).toUpperCase()} / {formatDate(ticket.createdAt)}</small></span>
                  <span className={`status status-${ticket.status.toLowerCase()}`}>{statusLabels[ticket.status] ?? ticket.status}</span>
                  <span className="row-chevron">›</span>
                </button>
              ))}
            </div>
          </section>

          <aside className="panel detail-panel">
            {selectedTicket ? (
              <>
                <div className="detail-top">
                  <span className={`status status-${selectedTicket.status.toLowerCase()}`}>{statusLabels[selectedTicket.status] ?? selectedTicket.status}</span>
                  <span className="ticket-id">#{selectedTicket.id.slice(0, 8).toUpperCase()}</span>
                </div>
                <h2>{selectedTicket.title}</h2>
                <p className="detail-description">{selectedTicket.description}</p>
                <div className="detail-meta">
                  <div><span>Erstellt</span><strong>{formatDate(selectedTicket.createdAt)}</strong></div>
                  <div><span>Kategorie</span><strong>{categoryLabels[selectedTicket.category] ?? selectedTicket.category}</strong></div>
                </div>
                <div className="assist-box">
                  <div className="assist-title"><span className="spark">✦</span><strong>KI-Klassifizierung</strong><span className="assist-tag">BETA</span></div>
                  <label className="provider-control">
                    <span>Provider</span>
                    <select value={provider} onChange={(event) => { if (isProvider(event.target.value)) setProvider(event.target.value); setClassification(null) }}>
                      <option value="OpenRouter">OpenRouter</option>
                      <option value="Claude">Claude</option>
                      <option value="Keyword">Keyword-Regeln (ohne KI)</option>
                    </select>
                  </label>
                  {classification ? (
                    <>
                      <div className="classification-result">
                        <strong>{categoryLabels[classification.category] ?? classification.category}</strong>
                        <span>{Math.round(classification.confidence * 100)}% Konfidenz</span>
                      </div>
                      {classification.isItRelated
                        ? <p>{classification.suggestedSolution}</p>
                        : <p>{classification.message ?? 'Kein IT-Bezug. Es wurde kein Lösungsvorschlag erstellt.'}</p>}
                      <p className="classification-source">{classification.source} · {classification.model}</p>
                    </>
                  ) : selectedHistory.length > 0 ? (
                    <>
                      <div className="classification-result">
                        <strong>{categoryLabels[selectedHistory[0].category] ?? selectedHistory[0].category}</strong>
                        <span>{Math.round(selectedHistory[0].confidence * 100)}% Konfidenz</span>
                      </div>
                      {selectedHistory[0].isItRelated
                        ? <p>{selectedHistory[0].suggestedSolution}</p>
                        : <p>Kein IT-Bezug. Es wurde kein Lösungsvorschlag erstellt.</p>}
                      <p className="classification-source">{selectedHistory[0].source} · {selectedHistory[0].model}</p>
                    </>
                  ) : (
                    <p>{historyLoading ? 'Klassifizierungshistorie wird geladen ...' : 'Die Assistenz ordnet das Ticket einer Kategorie zu und schlägt einen nächsten Schritt vor.'}</p>
                  )}
                  <button className="primary-button" onClick={() => void runClassification()} disabled={classifying || apiStatus !== 'connected'}>
                    {classifying ? 'Analysiere ...' : selectedHistory.length > 0 || classification ? 'Erneut klassifizieren' : 'Ticket klassifizieren'} <span>→</span>
                  </button>
                  {selectedHistory.length > 1 && <p className="history-note">{selectedHistory.length} Klassifizierungen gespeichert</p>}
                </div>
              </>
            ) : <div className="empty-state detail-empty">{loading ? 'Tickets werden geladen ...' : 'Wähle ein Ticket aus oder lege ein neues an.'}</div>}
          </aside>
        </div>

        <section className="create-section">
          <div className="create-intro">
            <p className="eyebrow">Neuer Vorgang</p>
            <h2>Ticket erfassen</h2>
            <p>Eine klare Beschreibung hilft der Assistenz bei der Einordnung.</p>
          </div>
          <form className="ticket-form" onSubmit={createTicket}>
            <label><span>Titel</span><input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Worum geht es?" /></label>
            <label><span>Beschreibung</span><textarea value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Beschreibe das Problem oder Anliegen ..." rows={3} /></label>
            <button className="primary-button" disabled={saving || !title.trim() || !description.trim() || apiStatus !== 'connected'}>{saving ? 'Wird angelegt ...' : 'Ticket anlegen'} <span>→</span></button>
          </form>
        </section>
        {notice && <div className="notice" role="status">{notice}</div>}
      </main>
      <footer><span>TicketDesk / Support Operations</span><span>Backend: .NET API · Provider: {provider}</span></footer>
    </div>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: 'short' }).format(new Date(value))
}

export default App
