import { API_BASE_URL } from '../../../common/api/config'
import type { ITimeWaveformResponse } from '../../import/types'

export const getTimeWaveforms = async (
  binCount: number,
  signalIds?: string[]
): Promise<ITimeWaveformResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/waveforms/time`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ binCount, signalIds: signalIds ?? [] }),
  })

  if (!response.ok) {
    throw new Error(await readWaveformError(response))
  }

  return response.json() as Promise<ITimeWaveformResponse>
}

const readWaveformError = async (response: Response) => {
  const fallback = 'Waveform data could not be generated.'
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

    if (typeof body?.message === 'string') {
      return body.message
    }
  } catch {
    return text
  }

  return fallback
}
