import { API_BASE_URL } from '../../../../common/api/config'
import type { IAgentQueryRequest, IAgentQueryResponse } from '../types/copilot.types'

export const postAgentQuery = async (request: IAgentQueryRequest): Promise<IAgentQueryResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/agent/query`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    const fallback = 'The investigation could not be completed.'
    try {
      const body = await response.json()
      if (Array.isArray(body?.errors)) {
        const reasons = body.errors
          .map((e: { reason?: string }) => e.reason)
          .filter(Boolean)
          .join('. ')
        throw new Error(reasons || fallback)
      }
      throw new Error(body?.message ?? fallback)
    } catch {
      throw new Error(fallback)
    }
  }

  return response.json() as Promise<IAgentQueryResponse>
}
