import { API_BASE_URL } from '../../../../common/api/config'
import type { IAgentQueryRequest, IAgentQueryResponse } from '../types/copilot.types'

export const postAgentQuery = async (request: IAgentQueryRequest): Promise<IAgentQueryResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/agent/query`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await readCopilotError(response))
  }

  return response.json() as Promise<IAgentQueryResponse>
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
