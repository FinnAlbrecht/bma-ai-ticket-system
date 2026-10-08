export type Ticket = {
  id: string
  title: string
  description: string
  status: string
  category: string
  createdAt: string
  classifiedAt: string | null
  resolvedAt: string | null
  resolutionNotes: string | null
  suggestedResolution: string | null
  solutionSourceTicketId: string | null
  createdByUserId: string | null
  createdByName: string | null
}

export type AuthUser = {
  id: string
  email: string
  displayName: string
}

export type SimilarSolution = {
  found: boolean
  sourceTicketId: string | null
  sourceTicketTitle: string | null
  category: string | null
  suggestedSolution: string | null
  similarity: number | null
  ticket: Ticket
}

export type DashboardMetrics = {
  totalTickets: number
  inProgressTickets: number
  resolvedTickets: number
  averageAiDurationMilliseconds: number
  totalAiDurationMilliseconds: number
  averageResolutionMilliseconds: number
  totalResolutionMilliseconds: number
  tickets: DashboardTicket[]
}

export type DashboardTicket = {
  id: string
  title: string
  status: string
  category: string
  createdAt: string
  resolvedAt: string | null
  aiDurationMilliseconds: number | null
}

export type ClassificationResult = {
  category: string
  confidence: number
  suggestedSolution: string
  duration: string
  source: string
  model: string
  isItRelated: boolean
  message: string | null
  createdAt: string
}

export type TicketClassification = Omit<ClassificationResult, 'message'>

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response

  try {
    response = await fetch(path, { ...init, credentials: 'include' })
  } catch {
    throw new Error('Backend nicht erreichbar. Starte zuerst die .NET-API auf Port 5278.')
  }

  if (!response.ok) {
    const body = await response.text()
    let message = body.trim()

    if (message.startsWith('"')) {
      try {
        message = JSON.parse(message) as string
      } catch {
        // Keep the original response text if it is not a JSON string.
      }
    } else if (message.startsWith('{')) {
      try {
        const details: unknown = JSON.parse(message)
        if (typeof details === 'object' && details !== null) {
          const problem = details as { detail?: unknown; title?: unknown }
          message = typeof problem.detail === 'string'
            ? problem.detail
            : typeof problem.title === 'string'
              ? problem.title
              : message
        }
      } catch {
        // Keep the original response text if it is not valid JSON.
      }
    }

    throw new ApiError(message || `API-Anfrage fehlgeschlagen (HTTP ${response.status}).`, response.status)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export class ApiError extends Error {
  public readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
    this.name = 'ApiError'
  }
}

export function getCurrentUser() {
  return request<AuthUser>('/api/auth/me')
}

export function registerUser(displayName: string, email: string, password: string) {
  return request<AuthUser>('/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ displayName, email, password }),
  })
}

export function loginUser(email: string, password: string) {
  return request<AuthUser>('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
}

export function logoutUser() {
  return request<void>('/api/auth/logout', { method: 'POST' })
}

export function getApiHealth() {
  return request<{ status: string }>('/api/test')
}

export function getTickets() {
  return request<Ticket[]>('/api/tickets')
}

export function deleteAllTickets(password: string) {
  return request<void>('/api/tickets', {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ password }),
  })
}

export function getDashboardMetrics() {
  return request<DashboardMetrics>('/api/dashboard/metrics')
}

export function createTicket(title: string, description: string) {
  return request<Ticket>('/api/tickets', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title, description }),
  })
}

export function findSimilarSolution(ticketId: string) {
  return request<SimilarSolution>(`/api/tickets/${ticketId}/similar-solution`, { method: 'POST' })
}

export function acceptSuggestedSolution(ticketId: string) {
  return request<Ticket>(`/api/tickets/${ticketId}/accept-suggested-solution`, { method: 'POST' })
}

export function resolveOutOfScopeTicket(ticketId: string) {
  return request<Ticket>(`/api/tickets/${ticketId}/resolve-out-of-scope`, { method: 'POST' })
}

export function classifyTicket(ticketId: string) {
  return request<ClassificationResult>(`/api/tickets/${ticketId}/classify?provider=OpenRouter`, { method: 'POST' })
}

export function getClassificationHistory(ticketId: string) {
  return request<TicketClassification[]>(`/api/tickets/${ticketId}/classifications`)
}
