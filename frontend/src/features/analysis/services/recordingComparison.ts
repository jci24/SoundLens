import { API_BASE_URL } from '../../../common/api/config'
import type { IRequestedRegionOfInterest } from '../utils/analysisWorkspaceState'
import type { IRecordingComparisonResponse } from '../types'

export const getRecordingComparison = async (
  recordingIdA: string,
  recordingIdB: string,
  regionOfInterest?: IRequestedRegionOfInterest | null
): Promise<IRecordingComparisonResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/comparisons/recordings`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      recordingIdA,
      recordingIdB,
      startTimeSeconds: regionOfInterest?.startTimeSeconds ?? null,
      endTimeSeconds: regionOfInterest?.endTimeSeconds ?? null,
    }),
  })

  if (!response.ok) {
    throw new Error(await readComparisonError(response))
  }

  return response.json() as Promise<IRecordingComparisonResponse>
}

const readComparisonError = async (response: Response) => {
  const fallback = 'Comparison results could not be prepared.'
  const text = await response.text()

  if (!text) {
    return fallback
  }

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

    if (typeof body?.message === 'string') {
      return body.message
    }
  } catch {
    return text
  }

  return fallback
}
