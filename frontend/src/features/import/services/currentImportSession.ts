import { API_BASE_URL } from '../../../common/api/config'
import type { IImportSessionResponse } from '../../../common/contracts/import'

const getCurrentImportSession = async (): Promise<IImportSessionResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/import/session`)

  if (!response.ok) {
    throw new Error('The current import session could not be restored.')
  }

  return response.json() as Promise<IImportSessionResponse>
}

export { getCurrentImportSession }
