export type Ticket = {
  id: string
  title: string
  description: string
  status: string
  category: string
  createdAt: string
  classifiedAt: string | null
}

export type Provider = 'OpenRouter' | 'Claude' | 'Keyword'

export type ClassificationResult = {
  category: string
  confidence: number
  suggestedSolution: string
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
    response = await fetch(path, init)
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

    throw new Error(message || `API-Anfrage fehlgeschlagen (HTTP ${response.status}).`)
  }

  return response.json() as Promise<T>
}

export function getApiHealth() {
  return request<{ status: string }>('/api/test')
}

export function getTickets() {
  return request<Ticket[]>('/api/tickets')
}

export function createTicket(title: string, description: string) {
  return request<Ticket>('/api/tickets', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title, description }),
  })
}

export function classifyTicket(ticketId: string, provider: Provider) {
  const query = new URLSearchParams({ provider })
  return request<ClassificationResult>(`/api/tickets/${ticketId}/classify?${query}`, { method: 'POST' })
}

export function getClassificationHistory(ticketId: string) {
  return request<TicketClassification[]>(`/api/tickets/${ticketId}/classifications`)
}
