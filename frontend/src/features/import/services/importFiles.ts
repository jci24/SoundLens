import { API_BASE_URL } from '../../../common/api/config'
import type { IImportFilesResponse, IImportRequest } from '../types'

export const importFiles = async (request: IImportRequest): Promise<IImportFilesResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/import`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await readImportError(response))
  }

  return response.json() as Promise<IImportFilesResponse>
}

const readImportError = async (response: Response) => {
  const fallback = 'Import failed.'
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

    if (body?.errors && typeof body.errors === 'object') {
      return Object.values(body.errors)
        .flatMap((error) => (Array.isArray(error) ? error : [error]))
        .filter((error): error is string => typeof error === 'string')
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
