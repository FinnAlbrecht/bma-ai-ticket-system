import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import {
  classifyTicket,
  createTicket,
  deleteAllTickets,
  findSimilarSolution,
  acceptSuggestedSolution,
  getDashboardMetrics,
  getApiHealth,
  getCurrentUser,
  getClassificationHistory,
  getTicketChatMessages,
  getUnreadTicketChatMessages,
  getTickets,
  loginUser,
  logoutUser,
  registerUser,
  resolveOutOfScopeTicket,
  sendTicketChatMessage,
  markTicketChatMessagesRead,
} from './api'
import { ApiError } from './api'
import type {
  AuthUser,
  ClassificationResult,
  DashboardMetrics,
  Ticket,
  TicketChatMessage,
  TicketClassification,
} from './api'
import './App.css'

type ApiStatus = 'checking' | 'connected' | 'disconnected'
type TicketFilter = 'Alle' | 'Offen' | 'In Bearbeitung' | 'Antwort gefunden' | 'Kein IT-Bezug' | 'Gelöst'
type TicketScope = 'Deine' | 'Alle'
type TicketNotification = {
  id: string
  ticketId: string
  title: string
  kind: 'answer-found' | 'resolved' | 'out-of-scope' | 'classification-failed' | 'chat-reply'
  message?: string
}

const statusLabels: Record<string, string> = {
  New: 'Offen',
  Classified: 'Klassifiziert',
  InProgress: 'In Bearbeitung',
  Resolved: 'Gelöst',
  Closed: 'Geschlossen',
  AnswerFound: 'Antwort gefunden',
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

const filters: TicketFilter[] = ['Alle', 'Offen', 'In Bearbeitung', 'Antwort gefunden', 'Kein IT-Bezug', 'Gelöst']

function isOpenTicket(ticket: Pick<Ticket, 'status' | 'category'>) {
  return ticket.category !== 'OutOfScope' && (ticket.status === 'New' || ticket.status === 'Classified')
}

function isUnresolvedOutOfScopeTicket(ticket: Pick<Ticket, 'status' | 'category'>) {
  return ticket.category === 'OutOfScope' && ticket.status !== 'Resolved' && ticket.status !== 'Closed'
}

function getOutOfScopeLabel(ticket: Pick<Ticket, 'status'>) {
  return ticket.status === 'Resolved' || ticket.status === 'Closed'
    ? 'Kein IT-Bezug · nicht technisch gelöst'
    : 'Kein IT-Bezug'
}

function getRouteTicketId() {
  const match = window.location.pathname.match(/^\/tickets\/([0-9a-f-]+)$/i)
  return match?.[1] ?? null
}

function isDashboardPath() {
  return window.location.pathname === '/dashboard'
}

function App() {
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [user, setUser] = useState<AuthUser | null>(null)
  const [authChecking, setAuthChecking] = useState(true)
  const [authError, setAuthError] = useState('')
  const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null)
  const [routeTicketId, setRouteTicketId] = useState<string | null>(getRouteTicketId)
  const [dashboardPage, setDashboardPage] = useState(isDashboardPath)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [filter, setFilter] = useState<TicketFilter>('Alle')
  const [scope, setScope] = useState<TicketScope>('Deine')
  const [apiStatus, setApiStatus] = useState<ApiStatus>('checking')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [deletingAllTickets, setDeletingAllTickets] = useState(false)
  const [deletePasswordOpen, setDeletePasswordOpen] = useState(false)
  const [deletePassword, setDeletePassword] = useState('')
  const [deletePasswordError, setDeletePasswordError] = useState('')
  const [classifyingIds, setClassifyingIds] = useState<Set<string>>(() => new Set())
  const [similarityCheckingIds, setSimilarityCheckingIds] = useState<Set<string>>(() => new Set())
  const [notice, setNotice] = useState('')
  const [history, setHistory] = useState<TicketClassification[]>([])
  const [historyTicketId, setHistoryTicketId] = useState<string | null>(null)
  const [notifications, setNotifications] = useState<TicketNotification[]>([])
  const [chatMessages, setChatMessages] = useState<TicketChatMessage[]>([])
  const [chatTicketId, setChatTicketId] = useState<string | null>(null)
  const [chatLoading, setChatLoading] = useState(false)
  const [sendingChatMessage, setSendingChatMessage] = useState(false)
  const [chatError, setChatError] = useState('')
  const knownTicketStatuses = useRef<Map<string, string> | null>(null)
  const knownChatMessageIds = useRef<Set<string>>(new Set())
  const workspaceGeneration = useRef(0)
  const selectedTicket = tickets.find((ticket) => ticket.id === routeTicketId)
  const canManageSelectedTicket = selectedTicket?.createdByUserId === user?.id
  const canChatSelectedTicket = canManageSelectedTicket && selectedTicket?.category !== 'OutOfScope'

  const enqueueNotification = useCallback((notification: TicketNotification) => {
    setNotifications((current) => current.some((item) => item.id === notification.id)
      ? current
      : [...current, notification])
  }, [])

  const refreshUnreadChatNotifications = useCallback(async () => {
    try {
      const unreadMessages = await getUnreadTicketChatMessages()
      unreadMessages.forEach((message) => enqueueNotification({
        id: `chat-reply:${message.messageId}`,
        ticketId: message.ticketId,
        title: message.ticketTitle,
        kind: 'chat-reply',
        message: message.content,
      }))
    } catch (error) {
      if (!(error instanceof ApiError && error.status === 401)) {
        setNotice(error instanceof Error ? error.message : 'Neue Chat-Nachrichten konnten nicht geladen werden.')
      }
    }
  }, [enqueueNotification])

  const loadTickets = useCallback(async () => {
    const generation = workspaceGeneration.current
    try {
      const loadedTickets = await getTickets()
      if (generation !== workspaceGeneration.current) return
      const previousStatuses = knownTicketStatuses.current
      if (previousStatuses) {
        loadedTickets.forEach((ticket) => {
          const previousStatus = previousStatuses.get(ticket.id)
          if (ticket.createdByUserId === user?.id
            && ticket.status === 'AnswerFound'
            && previousStatus !== 'AnswerFound') {
            enqueueNotification({
              id: `answer-found:${ticket.id}`,
              ticketId: ticket.id,
              title: ticket.title,
              kind: 'answer-found',
            })
          } else if (ticket.createdByUserId === user?.id
            && ticket.status === 'Resolved'
            && previousStatus !== 'Resolved') {
            enqueueNotification({
              id: `resolved:${ticket.id}`,
              ticketId: ticket.id,
              title: ticket.title,
              kind: 'resolved',
            })
          }
        })
      }
      knownTicketStatuses.current = new Map(loadedTickets.map((ticket) => [ticket.id, ticket.status]))
      setTickets(loadedTickets)
      setApiStatus('connected')
    } catch (error) {
      if (generation !== workspaceGeneration.current) return
      if (error instanceof ApiError && error.status === 401) {
        setUser(null)
        setTickets([])
        setMetrics(null)
        setNotifications([])
        knownTicketStatuses.current = null
        setAuthError('Deine Sitzung ist abgelaufen. Bitte melde dich erneut an.')
      }
      setApiStatus('disconnected')
      setNotice(error instanceof Error ? error.message : 'Verbindung zur API fehlgeschlagen.')
    } finally {
      setLoading(false)
    }
  }, [enqueueNotification, user?.id])

  useEffect(() => {
    let active = true

    async function restoreSession() {
      let restoredUser: AuthUser | null = null
      try {
        const currentUser = await getCurrentUser()
        if (active) {
          restoredUser = currentUser
          setUser(currentUser)
          setApiStatus('connected')
        }
      } catch (error) {
        if (active) {
          if (error instanceof ApiError && error.status === 401) {
            setUser(null)
          } else {
            setApiStatus('disconnected')
            setAuthError(error instanceof Error ? error.message : 'Backend nicht erreichbar.')
          }
        }
      } finally {
        if (active) {
          setAuthChecking(false)
          if (!restoredUser) setLoading(false)
        }
      }
    }

    void restoreSession()
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!user) {
      knownTicketStatuses.current = null
      return
    }

    let active = true

    async function initializeWorkspace() {
      try {
        const loadedTickets = await getTickets()
        if (active) {
          knownTicketStatuses.current = new Map(loadedTickets.map((ticket) => [ticket.id, ticket.status]))
          setTickets(loadedTickets)
          setApiStatus('connected')
        }
      } catch (error) {
        if (active) {
          if (error instanceof ApiError && error.status === 401) {
            setUser(null)
            setTickets([])
            setMetrics(null)
            setNotifications([])
            knownTicketStatuses.current = null
            setAuthError('Deine Sitzung ist abgelaufen. Bitte melde dich erneut an.')
          }
          setApiStatus('disconnected')
          setNotice(error instanceof Error ? error.message : 'Tickets konnten nicht geladen werden.')
        }
      } finally {
        if (active) setLoading(false)
      }

      try {
        const dashboardMetrics = await getDashboardMetrics()
        if (active) setMetrics(dashboardMetrics)
      } catch (error) {
        if (active) setNotice(error instanceof Error ? error.message : 'Dashboard-Kennzahlen konnten nicht geladen werden.')
      }

      if (active) await refreshUnreadChatNotifications()

      try {
        await getApiHealth()
        if (active) setApiStatus('connected')
      } catch {
        if (active) setApiStatus('disconnected')
      }
    }

    void initializeWorkspace()
    return () => { active = false }
  }, [refreshUnreadChatNotifications, user])

  useEffect(() => {
    const onPopState = () => {
      setRouteTicketId(getRouteTicketId())
      setDashboardPage(isDashboardPath())
    }
    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  useEffect(() => {
    if (!user) return
    let active = true
    let refreshInProgress = false

    const refreshVisibleTickets = async () => {
      if (!active || document.visibilityState === 'hidden' || refreshInProgress) return

      refreshInProgress = true
      try {
        await loadTickets()
        await refreshUnreadChatNotifications()
      } finally {
        refreshInProgress = false
      }
    }
    const refreshOnVisibilityChange = () => {
      if (document.visibilityState === 'visible') void refreshVisibleTickets()
    }

    const interval = window.setInterval(() => { void refreshVisibleTickets() }, 2000)
    window.addEventListener('focus', refreshVisibleTickets)
    document.addEventListener('visibilitychange', refreshOnVisibilityChange)
    return () => {
      active = false
      window.clearInterval(interval)
      window.removeEventListener('focus', refreshVisibleTickets)
      document.removeEventListener('visibilitychange', refreshOnVisibilityChange)
    }
  }, [loadTickets, refreshUnreadChatNotifications, user])

  useEffect(() => {
    if (!routeTicketId || !user || !canChatSelectedTicket) {
      knownChatMessageIds.current.clear()
      return
    }

    let active = true
    let refreshInProgress = false
    let initialized = false
    knownChatMessageIds.current = new Set()

    const refreshChat = async () => {
      if (!active || refreshInProgress || document.visibilityState === 'hidden') return
      refreshInProgress = true
      try {
        const messages = await getTicketChatMessages(routeTicketId)
        if (!active) return

        messages.forEach((message) => {
          const isNew = !knownChatMessageIds.current.has(message.id)
          if (message.role === 'assistant' && isNew && (initialized || !message.isRead)) {
            enqueueNotification({
              id: `chat-reply:${message.id}`,
              ticketId: routeTicketId,
              title: selectedTicket?.title ?? 'Dein Ticket',
              kind: 'chat-reply',
              message: message.content,
            })
          }
          knownChatMessageIds.current.add(message.id)
        })
        initialized = true
        setChatMessages(messages)
        setChatTicketId(routeTicketId)
        void markTicketChatMessagesRead(routeTicketId).catch((error: unknown) => {
          setChatError(error instanceof Error ? error.message : 'Die Nachricht konnte nicht als gelesen markiert werden.')
        })
      } catch (error) {
        if (active) {
          setChatTicketId(routeTicketId)
          setChatError(error instanceof Error ? error.message : 'Der Ticket-Chat konnte nicht geladen werden.')
        }
      } finally {
        if (active) setChatLoading(false)
        refreshInProgress = false
      }
    }

    void refreshChat()
    const interval = window.setInterval(() => { void refreshChat() }, 2000)
    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') void refreshChat()
    }
    document.addEventListener('visibilitychange', onVisibilityChange)
    return () => {
      active = false
      window.clearInterval(interval)
      document.removeEventListener('visibilitychange', onVisibilityChange)
    }
  }, [canChatSelectedTicket, enqueueNotification, routeTicketId, selectedTicket?.title, user])

  const selectedHistory = historyTicketId === routeTicketId ? history : []
  const historyLoading = Boolean(routeTicketId && historyTicketId !== routeTicketId)
  const visibleTickets = tickets.filter((ticket) => {
    if (scope === 'Deine' && ticket.createdByUserId !== user?.id) return false
    if (filter === 'Alle') return true
    if (filter === 'Offen') return isOpenTicket(ticket)
    if (filter === 'In Bearbeitung') return ticket.status === 'InProgress' && ticket.category !== 'OutOfScope'
    if (filter === 'Antwort gefunden') return ticket.status === 'AnswerFound'
    if (filter === 'Kein IT-Bezug') return isUnresolvedOutOfScopeTicket(ticket)
    return ticket.status === 'Resolved' || ticket.status === 'Closed'
  })
  const refreshMetrics = async () => {
    const generation = workspaceGeneration.current
    try {
      const nextMetrics = await getDashboardMetrics()
      if (generation === workspaceGeneration.current) setMetrics(nextMetrics)
    } catch (error) {
      if (generation === workspaceGeneration.current) {
        setNotice(error instanceof Error ? error.message : 'Dashboard-Kennzahlen konnten nicht geladen werden.')
      }
    }
  }

  const handleResolveOutOfScope = async (ticketId: string) => {
    try {
      const updatedTicket = await resolveOutOfScopeTicket(ticketId)
      setTickets((current) => current.map((ticket) => ticket.id === ticketId ? updatedTicket : ticket))
      knownTicketStatuses.current?.set(ticketId, updatedTicket.status)
      setNotifications((current) => current.filter((notification) => notification.ticketId !== ticketId))
      setNotice('Das Ticket ohne IT-Bezug wurde als gelöst markiert.')
      await refreshMetrics()
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Das Ticket konnte nicht abgeschlossen werden.')
    }
  }

  const handleDeleteAllTickets = async (event: FormEvent) => {
    event.preventDefault()
    const ticketCount = tickets.length
    if (ticketCount === 0 || saving || deletingAllTickets) return

    const confirmation = window.confirm(
      `Möchtest du wirklich alle ${ticketCount} Tickets einschließlich ihrer KI-Klassifizierungen endgültig löschen?`,
    )
    if (!confirmation) return

    setDeletingAllTickets(true)
    setDeletePasswordError('')
    try {
      await deleteAllTickets(deletePassword)
      workspaceGeneration.current += 1
      setTickets([])
      setMetrics(null)
      setNotifications([])
      setHistory([])
      setHistoryTicketId(null)
      setClassifyingIds(new Set())
      setSimilarityCheckingIds(new Set())
      knownTicketStatuses.current = new Map()
      if (routeTicketId) {
        window.history.replaceState({}, '', '/')
        setRouteTicketId(null)
        setDashboardPage(false)
      }
      setNotice(`${ticketCount} ${ticketCount === 1 ? 'Ticket wurde' : 'Tickets wurden'} gelöscht.`)
      setDeletePassword('')
      setDeletePasswordOpen(false)
      await refreshMetrics()
    } catch (error) {
      setDeletePasswordError(error instanceof Error ? error.message : 'Die Tickets konnten nicht gelöscht werden.')
    } finally {
      setDeletingAllTickets(false)
    }
  }

  const openDeletePasswordDialog = () => {
    setDeletePassword('')
    setDeletePasswordError('')
    setDeletePasswordOpen(true)
  }

  const closeDeletePasswordDialog = () => {
    if (deletingAllTickets) return
    setDeletePassword('')
    setDeletePasswordError('')
    setDeletePasswordOpen(false)
  }

  useEffect(() => {
    if (!routeTicketId || !user) return

    let active = true

    void getClassificationHistory(routeTicketId)
      .then((items) => {
        if (active) {
          setHistory(items)
          setHistoryTicketId(routeTicketId)
        }
      })
      .catch((error: unknown) => {
        if (active) {
          setHistory([])
          setHistoryTicketId(routeTicketId)
          setNotice(error instanceof Error ? error.message : 'Klassifizierungshistorie konnte nicht geladen werden.')
        }
      })

    return () => { active = false }
  }, [routeTicketId, user])

  const handleAuthSubmit = async (displayName: string, email: string, password: string) => {
    setAuthError('')
    setLoading(true)
    try {
      const signedInUser = authMode === 'register'
        ? await registerUser(displayName, email, password)
        : await loginUser(email, password)
      setTickets([])
      setMetrics(null)
      setNotifications([])
      knownTicketStatuses.current = null
      setUser(signedInUser)
      setApiStatus('connected')
      setNotice('')
      setRouteTicketId(null)
      window.history.replaceState({}, '', '/')
      setScope('Deine')
    } catch (error) {
      setAuthError(error instanceof Error ? error.message : 'Anmeldung fehlgeschlagen.')
      setLoading(false)
    }
  }

  const handleLogout = async () => {
    try {
      await logoutUser()
      setUser(null)
      setTickets([])
      setMetrics(null)
      setNotifications([])
      setRouteTicketId(null)
      window.history.replaceState({}, '', '/')
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Abmeldung fehlgeschlagen.')
    }
  }

  const openTicket = (ticketId: string) => {
    window.history.pushState({}, '', `/tickets/${ticketId}`)
    setRouteTicketId(ticketId)
    setNotice('')
  }

  const openDashboard = () => {
    window.history.pushState({}, '', '/dashboard')
    setRouteTicketId(null)
    setDashboardPage(true)
    setNotice('')
    void refreshMetrics()
  }

  const goToQueue = () => {
    window.history.pushState({}, '', '/')
    setRouteTicketId(null)
    setDashboardPage(false)
    setNotice('')
  }

  const runClassification = async (ticketId: string, ticketTitle?: string) => {
    const generation = workspaceGeneration.current
    setClassifyingIds((current) => new Set(current).add(ticketId))
    setNotice('')

    try {
      const result: ClassificationResult = await classifyTicket(ticketId)
      if (generation !== workspaceGeneration.current) return
      const savedResult: TicketClassification = {
        category: result.category,
        confidence: result.confidence,
        suggestedSolution: result.suggestedSolution,
        duration: result.duration,
        source: result.source,
        model: result.model,
        isItRelated: result.isItRelated,
        createdAt: result.createdAt,
      }
      setHistory((current) => historyTicketId === ticketId ? [savedResult, ...current] : current)
      if (historyTicketId !== ticketId) {
        setHistory([savedResult])
        setHistoryTicketId(ticketId)
      }
      setApiStatus('connected')
      await Promise.all([loadTickets(), refreshMetrics()])
      if (generation !== workspaceGeneration.current) return
      if (!result.isItRelated) {
        enqueueNotification({
          id: `out-of-scope:${ticketId}:${Date.now()}`,
          ticketId,
          title: ticketTitle ?? tickets.find((ticket) => ticket.id === ticketId)?.title ?? 'Ticket',
          kind: 'out-of-scope',
          message: result.message ?? 'Die KI konnte keinen IT-Bezug feststellen und hat keinen Lösungsvorschlag erstellt.',
        })
      }
    } catch (error) {
      if (generation !== workspaceGeneration.current) return
      await Promise.all([loadTickets(), refreshMetrics()])
      if (generation !== workspaceGeneration.current) return
      const message = error instanceof Error ? error.message : 'OpenRouter-Klassifizierung fehlgeschlagen.'
      const failedTicket = tickets.find((ticket) => ticket.id === ticketId)
      enqueueNotification({
        id: `classification-failed:${ticketId}:${Date.now()}`,
        ticketId,
        title: ticketTitle ?? failedTicket?.title ?? 'Ticket',
        kind: 'classification-failed',
        message,
      })
    } finally {
      setClassifyingIds((current) => {
        const next = new Set(current)
        next.delete(ticketId)
        return next
      })
    }
  }

  const findSolutionOrAskAi = async (ticketId: string) => {
    const generation = workspaceGeneration.current
    setSimilarityCheckingIds((current) => new Set(current).add(ticketId))
    setNotice('')

    try {
      const result = await findSimilarSolution(ticketId)
      if (generation !== workspaceGeneration.current) return
      setTickets((current) => current.map((ticket) => ticket.id === result.ticket.id ? result.ticket : ticket))
      knownTicketStatuses.current?.set(ticketId, result.ticket.status)
      setApiStatus('connected')
      if (result.found) {
        enqueueNotification({
          id: `answer-found:${ticketId}`,
          ticketId,
          title: result.ticket.title,
          kind: 'answer-found',
        })
      } else {
        await runClassification(ticketId, result.ticket.title)
      }
    } catch (error) {
      if (generation !== workspaceGeneration.current) return
      setNotice(error instanceof Error ? error.message : 'Ähnliche gelöste Tickets konnten nicht geprüft werden.')
    } finally {
      setSimilarityCheckingIds((current) => {
        const next = new Set(current)
        next.delete(ticketId)
        return next
      })
    }
  }

  const acceptPreviousSolution = async (ticketId: string) => {
    const generation = workspaceGeneration.current
    try {
      const updatedTicket = await acceptSuggestedSolution(ticketId)
      if (generation !== workspaceGeneration.current) return
      setTickets((current) => current.map((ticket) => ticket.id === ticketId ? updatedTicket : ticket))
      setNotice('Super, die frühere Lösung wurde als hilfreich bestätigt und das Ticket ist gelöst.')
      await Promise.all([loadTickets(), refreshMetrics()])
    } catch (error) {
      if (generation === workspaceGeneration.current) {
        setNotice(error instanceof Error ? error.message : 'Die Lösung konnte nicht bestätigt werden.')
      }
    }
  }

  const handleSendChatMessage = async (ticketId: string, message: string): Promise<boolean> => {
    if (selectedTicket?.id !== ticketId || selectedTicket.createdByUserId !== user?.id) {
      setChatError('Du kannst nur im Chat deines eigenen Tickets schreiben.')
      return false
    }
    if (selectedTicket.category === 'OutOfScope') {
      setChatError('Bei Tickets ohne IT-Bezug ist der Chat deaktiviert.')
      return false
    }

    setSendingChatMessage(true)
    setChatError('')
    try {
      const exchange = await sendTicketChatMessage(ticketId, message)
      setChatMessages((current) => {
        const next = [...current]
        for (const newMessage of [exchange.userMessage, exchange.assistantMessage]) {
          if (!next.some((item) => item.id === newMessage.id)) next.push(newMessage)
          knownChatMessageIds.current.add(newMessage.id)
        }
        return next
      })
      setChatTicketId(ticketId)
      enqueueNotification({
        id: `chat-reply:${exchange.assistantMessage.id}`,
        ticketId,
        title: selectedTicket.title,
        kind: 'chat-reply',
        message: exchange.assistantMessage.content,
      })
      void markTicketChatMessagesRead(ticketId).catch((error: unknown) => {
        setChatError(error instanceof Error ? error.message : 'Die Nachricht konnte nicht als gelesen markiert werden.')
      })
      return true
    } catch (error) {
      setChatError(error instanceof Error ? error.message : 'Deine Nachricht konnte nicht gesendet werden.')
      return false
    } finally {
      setSendingChatMessage(false)
    }
  }

  const dismissActiveNotification = () => {
    const notification = notifications[0]
    if (notification?.kind === 'chat-reply') {
      void markTicketChatMessagesRead(notification.ticketId).catch((error: unknown) => {
        setNotice(error instanceof Error ? error.message : 'Die Chat-Nachricht konnte nicht als gelesen markiert werden.')
      })
    }
    setNotifications((current) => current.slice(1))
  }

  const submitTicket = async (event: FormEvent) => {
    event.preventDefault()
    const cleanTitle = title.trim()
    const cleanDescription = description.trim()
    if (!cleanTitle || !cleanDescription) return
    setSaving(true)
    setNotice('')

    try {
      const ticket = await createTicket(cleanTitle, cleanDescription)
      setTickets((current) => [ticket, ...current])
      knownTicketStatuses.current?.set(ticket.id, ticket.status)
      setTitle('')
      setDescription('')
      setApiStatus('connected')
      setNotice('Ticket erstellt. Du bleibst in der Ticketübersicht; die KI bearbeitet es im Hintergrund.')
      void findSolutionOrAskAi(ticket.id)
    } catch (error) {
      setApiStatus('disconnected')
      setNotice(error instanceof Error ? error.message : 'Ticket konnte nicht angelegt werden.')
    } finally {
      setSaving(false)
    }
  }

  const refreshTickets = () => {
    setNotice('')
    void loadTickets()
    void refreshMetrics()
  }

  const activeNotification = notifications[0]

  if (authChecking) {
    return <div className="auth-shell"><div className="auth-card">Anmeldestatus wird geprüft ...</div></div>
  }

  if (!user) {
    return (
      <AuthPage
        mode={authMode}
        error={authError}
        onModeChange={(mode) => { setAuthMode(mode); setAuthError('') }}
        onSubmit={handleAuthSubmit}
      />
    )
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <button className="brand brand-button" onClick={goToQueue} aria-label="Zur Ticketübersicht">
          <span className="brand-mark">TD</span>
          <span><strong>TicketDesk</strong><small>Service Operations</small></span>
        </button>
        <nav className="main-nav" aria-label="Hauptmenü">
          <button className={!dashboardPage ? 'nav-tab active' : 'nav-tab'} onClick={goToQueue}>Tickets</button>
          <button className={dashboardPage ? 'nav-tab active' : 'nav-tab'} onClick={openDashboard}>Dashboard</button>
        </nav>
        <div className="topbar-actions">
          <div className={`api-status api-status-${apiStatus}`} role="status" aria-label={
            apiStatus === 'connected' ? 'API verbunden' : apiStatus === 'disconnected' ? 'API nicht erreichbar' : 'API-Verbindung wird geprüft'
          }>
            <span className="live-dot" />
            <span>{apiStatus === 'checking' ? 'API prüft ...' : apiStatus === 'connected' ? 'API verbunden' : 'API getrennt'}</span>
          </div>
          <span className="account-name">{user.displayName}</span>
          <button
            className="delete-all-button"
            onClick={openDeletePasswordDialog}
            disabled={tickets.length === 0 || saving || deletingAllTickets}
            title="Alle Tickets und KI-Klassifizierungen löschen"
          >
            {deletingAllTickets ? 'Wird gelöscht ...' : 'Alle löschen'}
          </button>
          <button className="logout-button" onClick={() => void handleLogout()}>Abmelden</button>
        </div>
      </header>
      <main>
        {dashboardPage ? (
          <DashboardPage metrics={metrics} loading={loading} onRefresh={refreshTickets} onOpenTicket={openTicket} />
        ) : selectedTicket ? (
          <TicketDetailPage
            ticket={selectedTicket}
            history={selectedHistory}
            historyLoading={historyLoading}
            searchingSimilarSolution={similarityCheckingIds.has(selectedTicket.id)}
            classifying={classifyingIds.has(selectedTicket.id)}
            canManageTicket={canManageSelectedTicket}
            canChatTicket={canChatSelectedTicket}
            chatMessages={chatTicketId === selectedTicket.id ? chatMessages : []}
            chatLoading={chatLoading || chatTicketId !== selectedTicket.id}
            sendingChatMessage={sendingChatMessage}
            chatError={chatTicketId === selectedTicket.id ? chatError : ''}
            onBack={goToQueue}
            onRetry={() => void runClassification(selectedTicket.id)}
            onAcceptSolution={() => void acceptPreviousSolution(selectedTicket.id)}
            onResolveOutOfScope={() => void handleResolveOutOfScope(selectedTicket.id)}
            onSendChatMessage={(message) => handleSendChatMessage(selectedTicket.id, message)}
          />
        ) : routeTicketId ? (
          <section className="ticket-detail-page">
            <button className="back-button" onClick={goToQueue}>← Zur Ticketübersicht</button>
            <div className="panel empty-state detail-not-found">
              {loading ? 'Ticket wird geladen ...' : 'Dieses Ticket wurde nicht gefunden.'}
            </div>
          </section>
        ) : (
          <>
            <section className="page-heading">
              <div>
                <p className="eyebrow">Support workspace</p>
                <h1>Tickets im Blick behalten.</h1>
                <p className="lede">Erfassen, priorisieren und automatisch mit OpenRouter klassifizieren lassen.</p>
              </div>
              <div className="heading-date">
                <span>HEUTE</span>
                <strong>{new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date())}</strong>
              </div>
            </section>

            <section className="panel ticket-panel queue-panel">
              <div className="panel-head">
                <div><p className="eyebrow">Queue</p><h2>Aktuelle Tickets</h2></div>
                <button className="icon-button" onClick={refreshTickets} title="Tickets aktualisieren" aria-label="Tickets aktualisieren">↻</button>
              </div>
              <div className="filter-row">
                <div className="owner-tabs" aria-label="Ticketbereich">
                  {(['Deine', 'Alle'] as const).map((option) => (
                    <button
                      key={option}
                      className={scope === option ? 'owner-tab active' : 'owner-tab'}
                      onClick={() => setScope(option)}
                    >
                      {option}<span>
                        {option === 'Deine'
                          ? tickets.filter((ticket) => ticket.createdByUserId === user.id).length
                          : tickets.length}
                      </span>
                    </button>
                  ))}
                </div>
                {filters.map((option) => (
                  <button key={option} className={filter === option ? 'filter active' : 'filter'} onClick={() => setFilter(option)}>
                    {option}<span>{tickets.filter((ticket) => {
                      if (scope === 'Deine' && ticket.createdByUserId !== user.id) return false
                      if (option === 'Alle') return true
                      if (option === 'Offen') return isOpenTicket(ticket)
                      if (option === 'In Bearbeitung') return ticket.status === 'InProgress' && ticket.category !== 'OutOfScope'
                      if (option === 'Antwort gefunden') return ticket.status === 'AnswerFound'
                      if (option === 'Kein IT-Bezug') return isUnresolvedOutOfScopeTicket(ticket)
                      return ticket.status === 'Resolved' || ticket.status === 'Closed'
                    }).length}</span>
                  </button>
                ))}
              </div>
              <div className="ticket-list">
                {loading && <div className="empty-state">Tickets werden geladen ...</div>}
                {!loading && visibleTickets.length === 0 && <div className="empty-state">{apiStatus === 'disconnected' ? 'Die API ist nicht erreichbar. Starte das Backend und aktualisiere die Liste.' : scope === 'Alle' ? 'Noch keine Team-Tickets in diesem Filter.' : 'Keine eigenen Tickets in diesem Filter. Wechsle zu „Alle“, um die Team-Tickets zu sehen.'}</div>}
                {visibleTickets.map((ticket) => (
                  <button key={ticket.id} className="ticket-row" onClick={() => openTicket(ticket.id)}>
                    <span className={`ticket-indicator ${classifyingIds.has(ticket.id) ? 'ticket-indicator-loading' : ''}`} />
                    <span className="ticket-copy">
                      <strong>{ticket.title}</strong>
                      <small>#{ticket.id.slice(0, 8).toUpperCase()} / {formatDate(ticket.createdAt)}</small>
                      <small>Erstellt von {ticket.createdByName ?? 'unbekannt'}</small>
                    </span>
                    <span className="ticket-row-badges">
                      <span className={`status status-${ticket.status.toLowerCase()}`}>
                        {similarityCheckingIds.has(ticket.id) ? 'Lösung wird gesucht ...' : classifyingIds.has(ticket.id) ? 'KI arbeitet ...' : statusLabels[ticket.status] ?? ticket.status}
                      </span>
                      {ticket.category === 'OutOfScope' && <span className="ticket-flag">{getOutOfScopeLabel(ticket)}</span>}
                    </span>
                    <span className="row-chevron">›</span>
                  </button>
                ))}
              </div>
            </section>

            <section className="create-section">
              <div className="create-intro">
                <p className="eyebrow">Neuer Vorgang</p>
                <h2>Ticket erfassen</h2>
                <p>Nach dem Erstellen sucht das System zuerst nach einer bewährten Lösung. Falls keine passt, fragt es OpenRouter.</p>
              </div>
              <form className="ticket-form" onSubmit={submitTicket}>
                <label><span>Titel</span><input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Worum geht es?" /></label>
                <label><span>Beschreibung</span><textarea value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Beschreibe das Problem oder Anliegen ..." rows={3} /></label>
                <button className="primary-button" disabled={saving || !title.trim() || !description.trim() || apiStatus !== 'connected'}>
                  {saving ? 'Wird angelegt ...' : 'Ticket anlegen'} <span>→</span>
                </button>
              </form>
            </section>
          </>
        )}
        {notice && <div className="notice" role="status" aria-live="polite">{notice}</div>}
      </main>
      <footer><span>TicketDesk / Support Operations</span><span>KI-Klassifizierung: OpenRouter</span></footer>
      {activeNotification && (
        <div className="notification-backdrop">
          <section className="notification-dialog" role="dialog" aria-modal="true" aria-labelledby="notification-title">
            <span className="notification-icon">
              {activeNotification.kind === 'answer-found'
                ? '✦'
                : activeNotification.kind === 'resolved'
                  ? '✓'
                  : activeNotification.kind === 'chat-reply'
                    ? '✉'
                    : '!'}
            </span>
            <p className="eyebrow">
              {activeNotification.kind === 'chat-reply'
                ? 'Neue Chat-Antwort'
                : activeNotification.kind === 'answer-found'
                  ? 'Antwort gefunden'
                  : activeNotification.kind === 'resolved'
                    ? 'Ticket gelöst'
                    : activeNotification.kind === 'out-of-scope'
                      ? 'Kein IT-Bezug'
                      : 'Klassifizierung fehlgeschlagen'}
            </p>
            <h2 id="notification-title">
              {activeNotification.kind === 'chat-reply'
                ? 'Die KI hat dir geantwortet.'
                : activeNotification.kind === 'answer-found'
                  ? 'Eine passende Lösung ist da.'
                  : activeNotification.kind === 'resolved'
                    ? 'Dein Ticket wurde gelöst.'
                    : activeNotification.kind === 'out-of-scope'
                      ? 'Das Ticket betrifft kein IT-Thema.'
                      : 'Das Ticket konnte nicht klassifiziert werden.'}
            </h2>
            <p className="notification-copy">
              {activeNotification.kind === 'chat-reply'
                ? `Eine neue Nachricht für „${activeNotification.title}“ ist eingetroffen.${activeNotification.message ? ` ${activeNotification.message.slice(0, 240)}${activeNotification.message.length > 240 ? '…' : ''}` : ''}`
                : activeNotification.kind === 'answer-found'
                  ? `Für „${activeNotification.title}“ wurde eine bewährte Lösung aus einem früheren Ticket gefunden. Du kannst sie im Ticket prüfen und bestätigen oder OpenRouter erneut fragen.`
                  : activeNotification.kind === 'resolved'
                    ? `„${activeNotification.title}“ wurde erfolgreich abgeschlossen.`
                    : activeNotification.kind === 'out-of-scope'
                      ? `„${activeNotification.title}“ wurde als „Kein IT-Bezug“ eingeordnet. Du kannst das Ticket als gelöst markieren oder die Details ansehen.`
                      : `„${activeNotification.title}“ ist noch offen. ${activeNotification.message ?? 'Die KI konnte keine Klassifizierung erstellen.'}`}
            </p>
            <div className="notification-actions">
              {activeNotification.kind === 'out-of-scope' ? (
                <button
                  className="primary-button"
                  onClick={() => {
                    const ticketId = activeNotification.ticketId
                    setNotifications((current) => current.slice(1))
                    void handleResolveOutOfScope(ticketId)
                  }}
                >
                  Als gelöst markieren <span>✓</span>
                </button>
              ) : activeNotification.kind === 'classification-failed' ? (
                <button
                  className="primary-button"
                  onClick={() => {
                    const ticketId = activeNotification.ticketId
                    setNotifications((current) => current.slice(1))
                    void runClassification(ticketId, activeNotification.title)
                  }}
                >
                  OpenRouter erneut fragen <span>↻</span>
                </button>
              ) : (
                <button
                  className="primary-button"
                  onClick={() => {
                    const ticketId = activeNotification.ticketId
                    setNotifications((current) => current.slice(1))
                    openTicket(ticketId)
                  }}
                >
                  Ticket ansehen <span>→</span>
                </button>
              )}
              {(activeNotification.kind === 'classification-failed' || activeNotification.kind === 'out-of-scope') && (
                <button
                  className="secondary-button"
                  onClick={() => {
                    const ticketId = activeNotification.ticketId
                    setNotifications((current) => current.slice(1))
                    openTicket(ticketId)
                  }}
                >
                  Ticket ansehen
                </button>
              )}
              <button className="secondary-button" onClick={dismissActiveNotification}>
                Später schließen
              </button>
            </div>
          </section>
        </div>
      )}
      {deletePasswordOpen && (
        <div className="delete-password-backdrop">
          <section className="delete-password-dialog" role="dialog" aria-modal="true" aria-labelledby="delete-password-title">
            <p className="eyebrow">Sicherheitsprüfung</p>
            <h2 id="delete-password-title">Alle Tickets löschen?</h2>
            <p>Zum Löschen aller {tickets.length} Tickets und KI-Klassifizierungen musst du das Löschpasswort eingeben.</p>
            <form onSubmit={(event) => void handleDeleteAllTickets(event)}>
              <label htmlFor="delete-password">Löschpasswort</label>
              <input
                id="delete-password"
                type="password"
                value={deletePassword}
                onChange={(event) => setDeletePassword(event.target.value)}
                autoComplete="current-password"
                maxLength={256}
                required
                autoFocus
              />
              {deletePasswordError && <p className="auth-error" role="alert">{deletePasswordError}</p>}
              <div className="delete-password-actions">
                <button type="button" className="secondary-button" onClick={closeDeletePasswordDialog} disabled={deletingAllTickets}>
                  Abbrechen
                </button>
                <button type="submit" className="delete-all-button" disabled={deletingAllTickets || !deletePassword}>
                  {deletingAllTickets ? 'Wird gelöscht ...' : 'Alle endgültig löschen'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </div>
  )
}

type DashboardPageProps = {
  metrics: DashboardMetrics | null
  loading: boolean
  onRefresh: () => void
  onOpenTicket: (ticketId: string) => void
}

function DashboardPage({ metrics, loading, onRefresh, onOpenTicket }: DashboardPageProps) {
  const dashboardTickets = Array.isArray(metrics?.tickets) ? metrics.tickets : []
  const hasTicketBreakdown = metrics !== null && Array.isArray(metrics.tickets)

  return (
    <section className="dashboard-page">
      <div className="dashboard-heading">
        <div>
          <p className="eyebrow">Auswertung</p>
          <h1>KI-Ticket-Dashboard</h1>
          <p className="lede">Bearbeitungszeiten und gelöste Tickets im Überblick.</p>
        </div>
        <button className="icon-button" onClick={onRefresh} title="Dashboard aktualisieren" aria-label="Dashboard aktualisieren">↻</button>
      </div>

      <div className="dashboard-metrics">
        <MetricCard label="Tickets gesamt" value={String(metrics?.totalTickets ?? '–')} note="alle erfassten Tickets" />
        <MetricCard label="Gelöst" value={String(metrics?.resolvedTickets ?? '–')} note="mit Lösung abgeschlossen" accent />
        <MetricCard label="In Bearbeitung" value={String(metrics?.inProgressTickets ?? '–')} note="KI, Support oder Antwortprüfung" />
        <MetricCard label="KI-Zeit gesamt" value={formatDuration(metrics?.totalAiDurationMilliseconds)} note="Summe pro Ticket" />
        <MetricCard label="KI-Zeit Ø / Ticket" value={formatDuration(metrics?.averageAiDurationMilliseconds)} note="Durchschnitt je KI-Antwort" />
        <MetricCard label="Lösungszeit gesamt" value={formatDuration(metrics?.totalResolutionMilliseconds)} note="Erstellung bis Lösung" accent />
        <MetricCard label="Ø Geschwindigkeit / Ticket" value={formatDuration(metrics?.averageResolutionMilliseconds)} note="durchschnittliche Lösungszeit" />
      </div>

      <section className="panel dashboard-table-panel">
        <div className="panel-head">
          <div><p className="eyebrow">Einzelauswertung</p><h2>KI-Zeit pro Ticket</h2></div>
          <span className="dashboard-table-count">{dashboardTickets.length} Tickets</span>
        </div>
        {loading && !metrics ? <div className="empty-state">Dashboard wird geladen ...</div> : !metrics || dashboardTickets.length === 0 ? (
          <div className="empty-state">
            {metrics && !hasTicketBreakdown
              ? 'Bitte starte das Backend neu, damit es die KI-Zeit pro Ticket an das Dashboard liefert.'
              : 'Noch keine Tickets für die Auswertung vorhanden.'}
          </div>
        ) : (
          <div className="dashboard-table-scroll">
            <table className="dashboard-table">
              <thead>
                <tr><th>Ticket</th><th>Status</th><th>KI-Bearbeitungszeit</th><th>Gesamte Lösungszeit</th></tr>
              </thead>
              <tbody>
                {dashboardTickets.map((ticket) => {
                  const resolutionTime = ticket.resolvedAt
                    ? Date.parse(ticket.resolvedAt) - Date.parse(ticket.createdAt)
                    : null
                  return (
                    <tr key={ticket.id} onClick={() => onOpenTicket(ticket.id)} tabIndex={0} onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault()
                        onOpenTicket(ticket.id)
                      }
                    }}>
                      <td><strong>{ticket.title}</strong><small>#{ticket.id.slice(0, 8).toUpperCase()}</small></td>
                      <td>
                        <span className={`status status-${ticket.status.toLowerCase()}`}>{statusLabels[ticket.status] ?? ticket.status}</span>
                        {ticket.category === 'OutOfScope' && <span className="ticket-flag">{getOutOfScopeLabel(ticket)}</span>}
                      </td>
                      <td>{ticket.aiDurationMilliseconds === null ? 'Noch keine KI-Zeit' : formatDuration(ticket.aiDurationMilliseconds)}</td>
                      <td>{resolutionTime === null ? 'Noch nicht gelöst' : formatDuration(resolutionTime)}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </section>
  )
}

type MetricCardProps = {
  label: string
  value: string
  note: string
  accent?: boolean
}

function MetricCard({ label, value, note, accent = false }: MetricCardProps) {
  return (
    <article className={`dashboard-metric-card ${accent ? 'accent' : ''}`}>
      <span className="metric-label">{label}</span>
      <strong>{value}</strong>
      <span className="metric-note">{note}</span>
    </article>
  )
}

type TicketDetailPageProps = {
  ticket: Ticket
  history: TicketClassification[]
  historyLoading: boolean
  searchingSimilarSolution: boolean
  classifying: boolean
  canManageTicket: boolean
  canChatTicket: boolean
  chatMessages: TicketChatMessage[]
  chatLoading: boolean
  sendingChatMessage: boolean
  chatError: string
  onBack: () => void
  onRetry: () => void
  onAcceptSolution: () => void
  onResolveOutOfScope: () => void
  onSendChatMessage: (message: string) => Promise<boolean>
}

function TicketDetailPage({
  ticket,
  history,
  historyLoading,
  searchingSimilarSolution,
  classifying,
  canManageTicket,
  canChatTicket,
  chatMessages,
  chatLoading,
  sendingChatMessage,
  chatError,
  onBack,
  onRetry,
  onAcceptSolution,
  onResolveOutOfScope,
  onSendChatMessage,
}: TicketDetailPageProps) {
  const latest = history[0]
  const [chatDraft, setChatDraft] = useState('')

  const submitChatMessage = async (event: FormEvent) => {
    event.preventDefault()
    const message = chatDraft.trim()
    if (!message || sendingChatMessage) return
    if (await onSendChatMessage(message)) setChatDraft('')
  }

  return (
    <section className="ticket-detail-page">
      <button className="back-button" onClick={onBack}>← Zur Ticketübersicht</button>
      <div className="detail-page-heading">
        <div>
          <p className="eyebrow">Ticket / {ticket.id.slice(0, 8).toUpperCase()}</p>
          <h1>{ticket.title}</h1>
        </div>
        <span className="detail-status-badges">
          <span className={`status status-${ticket.status.toLowerCase()}`}>{statusLabels[ticket.status] ?? ticket.status}</span>
          {ticket.category === 'OutOfScope' && <span className="ticket-flag">{getOutOfScopeLabel(ticket)}</span>}
        </span>
      </div>
      {canManageTicket && ticket.category === 'OutOfScope' && ticket.status !== 'Resolved' && ticket.status !== 'Closed' && (
        <div className="out-of-scope-banner">
          <div><strong>Kein IT-Bezug</strong><span>Wenn das Anliegen damit erledigt ist, kannst du das Ticket als gelöst markieren.</span></div>
          <button className="primary-button" onClick={onResolveOutOfScope}>Als gelöst markieren <span>✓</span></button>
        </div>
      )}
      <div className="detail-page-grid">
        <article className="panel ticket-description-panel">
          <p className="eyebrow">Beschreibung</p>
          <p className="detail-description">{ticket.description}</p>
          <div className="detail-meta">
            <div><span>Erstellt</span><strong>{formatDate(ticket.createdAt)}</strong></div>
            <div><span>Kategorie</span><strong>{categoryLabels[ticket.category] ?? ticket.category}</strong></div>
            <div><span>KI-Bearbeitungszeit</span><strong>{latest ? formatTimeSpan(latest.duration) : 'Noch offen'}</strong></div>
            <div><span>Gesamte Lösungszeit</span><strong>{ticket.resolvedAt ? formatDuration(Date.parse(ticket.resolvedAt) - Date.parse(ticket.createdAt)) : 'Noch nicht gelöst'}</strong></div>
          </div>
        </article>
        <article className="panel solution-panel">
          <div className="assist-title"><span className="spark">✦</span><strong>KI-Analyse und Lösungsvorschlag</strong></div>
          {classifying ? (
            <div className="solution-pending" role="status">
              <span className="loading-spinner" />
              <div><strong>OpenRouter analysiert dein Ticket ...</strong><p>Klassifizierung und Lösungsvorschlag werden erstellt.</p></div>
            </div>
          ) : latest ? (
            <>
              <div className="solution-category">
                <div><span className="solution-label">Klassifizierung</span><strong>{categoryLabels[latest.category] ?? latest.category}</strong></div>
                <span className="confidence">{Math.round(latest.confidence * 100)}% Konfidenz</span>
              </div>
              <div className="solution-content">
                <span className="solution-label">{latest.isItRelated ? 'Lösungsvorschlag' : 'Hinweis'}</span>
                <p>{latest.isItRelated ? latest.suggestedSolution : 'Dieses Anliegen hat keinen IT-Bezug. Es wurde deshalb kein technischer Lösungsvorschlag erstellt.'}</p>
              </div>
              <p className="classification-source">{latest.source} · {latest.model} · {formatTimeSpan(latest.duration)} · {formatDate(latest.createdAt)}</p>
            </>
          ) : ticket.suggestedResolution && ticket.solutionSourceTicketId ? (
            <>
              <div className="solution-category">
                <div><span className="solution-label">Frühere Lösung gefunden</span><strong>{categoryLabels[ticket.category] ?? ticket.category}</strong></div>
                <span className="confidence">Ähnliche Anfrage</span>
              </div>
              <div className="solution-content">
                <span className="solution-label">
                  {ticket.status === 'Resolved' ? 'Als hilfreich bestätigt' : `Lösung aus Ticket #${ticket.solutionSourceTicketId.slice(0, 8).toUpperCase()}`}
                </span>
                <p>{ticket.suggestedResolution}</p>
              </div>
              {canManageTicket && ticket.status !== 'Resolved' && (
                <div className="reuse-actions">
                  <button className="primary-button" onClick={onAcceptSolution}>Hat geholfen – Ticket lösen <span>✓</span></button>
                  <button className="secondary-button" onClick={onRetry} disabled={classifying}>Hat nicht geholfen – OpenRouter erneut fragen <span>↻</span></button>
                </div>
              )}
            </>
          ) : searchingSimilarSolution ? (
            <div className="solution-pending" role="status">
              <span className="loading-spinner" />
              <div><strong>Suche nach einer bewährten Lösung ...</strong><p>Ähnliche gelöste Tickets werden geprüft.</p></div>
            </div>
          ) : historyLoading ? (
            <div className="solution-pending"><span className="loading-spinner" /><div><strong>Ergebnis wird geladen ...</strong></div></div>
          ) : (
            <div className="solution-pending">
              <div><strong>Noch kein Ergebnis vorhanden.</strong><p>Es wurde keine passende frühere Lösung gefunden. OpenRouter kann das Ticket klassifizieren.</p></div>
              {canManageTicket && (
                <button className="primary-button" onClick={onRetry} disabled={classifying}>OpenRouter starten <span>→</span></button>
              )}
            </div>
          )}
          {history.length > 1 && <p className="history-note">{history.length} Klassifizierungen gespeichert</p>}
        </article>
      </div>
      {ticket.category !== 'OutOfScope' && latest?.isItRelated !== false && <section className="panel ticket-chat-panel" aria-label="Chat zum Ticket">
        <div className="panel-head">
          <div><p className="eyebrow">Rückfragen</p><h2>Mit der KI schreiben</h2></div>
          <span className="chat-owner-label">{canChatTicket ? 'Privater Ticket-Chat' : 'Nur für den Ersteller'}</span>
        </div>
        {canChatTicket ? (
          <>
            <div className="ticket-chat-messages" aria-live="polite" aria-relevant="additions text">
              {chatLoading && chatMessages.length === 0 && <p className="empty-state">Chat wird geladen ...</p>}
              {!chatLoading && chatMessages.length === 0 && (
                <p className="empty-state">Stelle eine Rückfrage zur KI-Antwort oder beschreibe, was du bereits ausprobiert hast.</p>
              )}
              {chatMessages.map((message) => (
                <article key={message.id} className={`chat-message chat-message-${message.role}`}>
                  <span>{message.role === 'assistant' ? 'TicketDesk KI' : 'Du'}</span>
                  <p>{message.content}</p>
                  <time dateTime={message.createdAt}>{formatDate(message.createdAt)}</time>
                </article>
              ))}
            </div>
            {chatError && <p className="auth-error chat-error" role="alert">{chatError}</p>}
            <form className="ticket-chat-form" onSubmit={(event) => void submitChatMessage(event)}>
              <label htmlFor="ticket-chat-message">Deine Nachricht</label>
              <textarea
                id="ticket-chat-message"
                value={chatDraft}
                onChange={(event) => setChatDraft(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey && !sendingChatMessage) {
                    event.preventDefault()
                    event.currentTarget.form?.requestSubmit()
                  }
                }}
                placeholder="Schreibe der KI, was noch unklar ist ..."
                maxLength={4000}
                rows={3}
                disabled={sendingChatMessage}
                required
              />
              <button className="primary-button" type="submit" disabled={sendingChatMessage || !chatDraft.trim()}>
                {sendingChatMessage ? 'KI antwortet ...' : 'Nachricht senden'} <span>→</span>
              </button>
            </form>
          </>
        ) : (
          <p className="chat-private-notice">Chatverlauf und Ticket-Aktionen sind nur für den Ersteller dieses Tickets zugänglich.</p>
        )}
      </section>}
    </section>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(value))
}

function formatDuration(milliseconds: number | undefined) {
  if (milliseconds === undefined) return '–'
  if (milliseconds < 1000) return `${Math.max(0, Math.round(milliseconds))} ms`

  const totalSeconds = Math.round(milliseconds / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  if (hours > 0) return `${hours} Std. ${minutes} Min.`
  if (minutes > 0) return `${minutes} Min. ${seconds} Sek.`
  return `${seconds} Sek.`
}

function formatTimeSpan(value: string) {
  const match = value.match(/^(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d+))?$/)
  if (!match) return value

  const days = Number(match[1] ?? 0)
  const hours = Number(match[2])
  const minutes = Number(match[3])
  const seconds = Number(match[4])
  const fraction = Number(`0.${match[5] ?? 0}`)
  const milliseconds = ((days * 24 + hours) * 3600 + minutes * 60 + seconds + fraction) * 1000
  return formatDuration(milliseconds)
}

export default App

type AuthPageProps = {
  mode: 'login' | 'register'
  error: string
  onModeChange: (mode: 'login' | 'register') => void
  onSubmit: (displayName: string, email: string, password: string) => Promise<void>
}

function AuthPage({ mode, error, onModeChange, onSubmit }: AuthPageProps) {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSubmitting(true)
    try {
      await onSubmit(displayName, email, password)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="auth-shell">
      <section className="auth-card">
        <div className="auth-brand"><span className="brand-mark">TD</span><strong>TicketDesk</strong></div>
        <p className="eyebrow">Gemeinsamer Support-Workspace</p>
        <h1>{mode === 'register' ? 'Konto erstellen' : 'Willkommen zurück.'}</h1>
        <p className="auth-copy">
          {mode === 'register'
            ? 'Registriere dich, damit Tickets dir zugeordnet und im Team geteilt werden können.'
            : 'Melde dich an, um deine Tickets und die Teamübersicht zu sehen.'}
        </p>
        <form className="auth-form" onSubmit={(event) => void submit(event)}>
          {mode === 'register' && (
            <label><span>Anzeigename</span><input value={displayName} onChange={(event) => setDisplayName(event.target.value)} autoComplete="name" minLength={2} maxLength={100} required /></label>
          )}
          <label><span>E-Mail</span><input type="email" value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="email" maxLength={320} required /></label>
          <label><span>Passwort</span><input type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete={mode === 'register' ? 'new-password' : 'current-password'} minLength={mode === 'register' ? 10 : 1} maxLength={128} required /></label>
          {mode === 'register' && <small className="password-hint">Mindestens 10 Zeichen.</small>}
          {error && <p className="auth-error" role="alert">{error}</p>}
          <button className="primary-button" disabled={submitting}>
            {submitting ? 'Einen Moment ...' : mode === 'register' ? 'Konto erstellen' : 'Anmelden'} <span>→</span>
          </button>
        </form>
        <p className="auth-switch">
          {mode === 'register' ? 'Schon ein Konto?' : 'Noch kein Konto?'}
          <button onClick={() => onModeChange(mode === 'register' ? 'login' : 'register')}>
            {mode === 'register' ? 'Anmelden' : 'Konto erstellen'}
          </button>
        </p>
      </section>
    </div>
  )
}
