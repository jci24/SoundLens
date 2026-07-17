import { API_BASE_URL } from '../../../common/api/config'
import type { IImportedRecordingInventoryResponse } from '../../../common/contracts/import'

const getImportedRecordingInventory = async (): Promise<IImportedRecordingInventoryResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/import/session/recordings`)

  if (!response.ok) {
    throw new Error('The recording inventory could not be loaded.')
  }

  return response.json() as Promise<IImportedRecordingInventoryResponse>
}

export { getImportedRecordingInventory }
