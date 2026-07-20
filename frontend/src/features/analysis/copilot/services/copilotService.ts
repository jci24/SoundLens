import { API_BASE_URL } from '../../../../common/api/config'
import type {
  IAgentActivityEvent,
  IAgentQueryRequest,
  IAgentQueryResponse,
} from '../types/copilot.types'
import { isStructuredObservationCollection } from './structuredObservationValidation'
import { isInvestigationPlan } from './investigationPlanValidation'

interface IAgentStreamEnvelope {
  eventType: 'activity' | 'result' | 'error'
  activity?: IAgentActivityEvent
  response?: IAgentQueryResponse
  message?: string
}

export const streamAgentQuery = async (
  request: IAgentQueryRequest,
  onActivity: (activity: IAgentActivityEvent) => void,
  signal?: AbortSignal
): Promise<IAgentQueryResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/agent/query/stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal,
  })

  if (!response.ok) {
    throw new Error(await readCopilotError(response))
  }

  if (!response.body) {
    throw new Error('The investigation stream was unavailable.')
  }
  if (!response.headers.get('Content-Type')?.includes('text/event-stream')) {
    throw new Error('The investigation stream returned an unexpected response type.')
  }

  return readAgentStream(response.body, onActivity)
}

export const readAgentStream = async (
  stream: ReadableStream<Uint8Array>,
  onActivity: (activity: IAgentActivityEvent) => void
): Promise<IAgentQueryResponse> => {
  const reader = stream.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let result: IAgentQueryResponse | null = null

  const consumeEvent = (eventText: string) => {
    const data = eventText
      .split(/\r?\n/)
      .filter((line) => line.startsWith('data:'))
      .map((line) => line.slice(5).trimStart())
      .join('\n')

    if (!data) return

    let envelope: IAgentStreamEnvelope
    try {
      envelope = JSON.parse(data) as IAgentStreamEnvelope
    } catch {
      throw new Error('The investigation stream returned malformed activity data.')
    }

    if (envelope.eventType === 'activity' && isAgentActivityEvent(envelope.activity)) {
      onActivity(envelope.activity)
      return
    }
    if (envelope.eventType === 'result' && isAgentQueryResponse(envelope.response) && !result) {
      result = envelope.response
      return
    }
    if (envelope.eventType === 'error') {
      throw new Error(envelope.message || 'The investigation could not be completed.')
    }
    throw new Error('The investigation stream returned an invalid event.')
  }

  try {
    while (true) {
      const { done, value } = await reader.read()
      buffer += decoder.decode(value, { stream: !done }).replace(/\r\n/g, '\n')

      let boundary = buffer.indexOf('\n\n')
      while (boundary >= 0) {
        consumeEvent(buffer.slice(0, boundary))
        buffer = buffer.slice(boundary + 2)
        boundary = buffer.indexOf('\n\n')
      }

      if (done) break
    }

    if (buffer.trim()) consumeEvent(buffer)
  } finally {
    reader.releaseLock()
  }

  if (!result) {
    throw new Error('The investigation stream ended before a validated response was received.')
  }
  return result
}

const isAgentActivityEvent = (value: unknown): value is IAgentActivityEvent => {
  if (!value || typeof value !== 'object') return false
  const activity = value as Partial<IAgentActivityEvent>
  return Number.isInteger(activity.sequence) && Number(activity.sequence) > 0 &&
    ['plan', 'routing', 'tool', 'evidence_check', 'fallback', 'completion', 'failure'].includes(activity.kind ?? '') &&
    ['running', 'completed', 'failed'].includes(activity.status ?? '') &&
    typeof activity.title === 'string' && activity.title.trim().length > 0 &&
    typeof activity.summary === 'string' && activity.summary.trim().length > 0
}

const isAgentQueryResponse = (value: unknown): value is IAgentQueryResponse => {
  if (!value || typeof value !== 'object') return false
  const response = value as Partial<IAgentQueryResponse>
  return typeof response.answer === 'string' &&
    Array.isArray(response.citedEvidence) &&
    Array.isArray(response.limitations) &&
    Array.isArray(response.nextSteps) &&
    Array.isArray(response.toolsUsed) &&
    isExternalCitationCollection(response.externalCitations, response.answer) &&
    isEvidenceSufficiency(response.evidenceSufficiency) &&
    isStructuredObservationCollection(response.structuredObservations) &&
    isInvestigationPlan(response.investigationPlan)
}

const isExternalCitationCollection = (value: unknown, answer: string) => {
  if (value == null) return true
  if (!Array.isArray(value)) return false

  return value.every((item) => {
    if (!item || typeof item !== 'object') return false
    const citation = item as Record<string, unknown>
    const metadata = citation.sourceMetadata
    if (!metadata || typeof metadata !== 'object') return false
    const sourceMetadata = metadata as Record<string, unknown>

    let url: URL
    try {
      url = new URL(String(citation.url))
    } catch {
      return false
    }

    return hasExactKeys(citation, ['title', 'url', 'startIndex', 'endIndex', 'sourceMetadata']) &&
      hasExactKeys(sourceMetadata, ['publisherHost', 'sourceClass', 'accessStatus', 'applicabilityStatus']) &&
      typeof citation.title === 'string' && citation.title.trim().length > 0 &&
      ['http:', 'https:'].includes(url.protocol) &&
      Number.isInteger(citation.startIndex) && Number(citation.startIndex) >= 0 &&
      Number.isInteger(citation.endIndex) && Number(citation.endIndex) > Number(citation.startIndex) &&
      Number(citation.endIndex) <= answer.length &&
      sourceMetadata.publisherHost === url.hostname.toLowerCase() &&
      ['standards_body', 'public_authority', 'unclassified'].includes(String(sourceMetadata.sourceClass)) &&
      sourceMetadata.accessStatus === 'not_verified' &&
      sourceMetadata.applicabilityStatus === 'not_assessed'
  })
}

const isEvidenceSufficiency = (value: unknown) => {
  if (value == null) return true
  if (typeof value !== 'object') return false

  const sufficiency = value as Record<string, unknown>
  return typeof sufficiency.intent === 'string' && sufficiency.intent.trim().length > 0 &&
    ['supported', 'partial', 'missing', 'contradicted', 'unavailable'].includes(String(sufficiency.status)) &&
    typeof sufficiency.label === 'string' && sufficiency.label.trim().length > 0 &&
    typeof sufficiency.reason === 'string' && sufficiency.reason.trim().length > 0 &&
    isStringArray(sufficiency.requiredEvidence) &&
    isStringArray(sufficiency.availableEvidence) &&
    isStringArray(sufficiency.limitationCodes)
}

const isStringArray = (value: unknown): value is string[] =>
  Array.isArray(value) && value.every((item) => typeof item === 'string')

const hasExactKeys = (value: Record<string, unknown>, keys: string[]) => {
  const actualKeys = Object.keys(value)
  return actualKeys.length === keys.length && keys.every((key) => actualKeys.includes(key))
}

const readCopilotError = async (response: Response) => {
  const fallback = 'The investigation could not be completed.'
  const text = await response.text()

  if (!text) return fallback

  try {
    const body = JSON.parse(text)

    if (Array.isArray(body?.errors)) {
      return body.errors
        .map((error: { reason?: string }) => error.reason)
        .filter(Boolean)
        .join('. ') || fallback
    }

    if (Array.isArray(body?.errors?.generalErrors)) {
      return body.errors.generalErrors
        .filter((error: unknown): error is string => typeof error === 'string')
        .join('. ') || fallback
    }

    return typeof body?.message === 'string' ? body.message : fallback
  } catch {
    return text
  }
}
