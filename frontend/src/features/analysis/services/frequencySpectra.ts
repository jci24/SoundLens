import { API_BASE_URL } from '../../../common/api/config'
import { isMessagePackResponse, readMessagePack } from '../../../common/api/messagePack'
import type { IRequestedRegionOfInterest } from '../utils/analysisWorkspaceState'
import type { IFrequencySpectrumResponse } from '../types'

export const getFrequencySpectra = async (
  fftSize: number,
  signalIds?: string[],
  regionOfInterest?: IRequestedRegionOfInterest | null
): Promise<IFrequencySpectrumResponse> => {
  const binCount = Math.floor(fftSize / 2) + 1
  const response = await fetch(`${API_BASE_URL}/api/spectra/frequency`, {
    method: 'POST',
    headers: {
      Accept: 'application/x-msgpack, application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      binCount,
      fftSize,
      signalIds: signalIds ?? [],
      startTimeSeconds: regionOfInterest?.startTimeSeconds ?? null,
      endTimeSeconds: regionOfInterest?.endTimeSeconds ?? null,
    }),
  })

  if (!response.ok) {
    throw new Error(await readSpectrumError(response))
  }

  if (isMessagePackResponse(response)) {
    return readMessagePack<IFrequencySpectrumResponse>(response)
  }

  return response.json() as Promise<IFrequencySpectrumResponse>
}

const readSpectrumError = async (response: Response) => {
  const fallback = 'Spectrum data could not be generated.'
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
