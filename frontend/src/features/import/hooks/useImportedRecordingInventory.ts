import { useCallback, useEffect, useState } from 'react'
import type { IImportedRecordingInventoryResponse } from '../../../common/contracts/import'
import { getImportedRecordingInventory } from '../services/importedRecordingInventory'

type TRecordingInventoryStatus = 'loading' | 'ready' | 'error'

const useImportedRecordingInventory = () => {
  const [inventory, setInventory] = useState<IImportedRecordingInventoryResponse | null>(null)
  const [status, setStatus] = useState<TRecordingInventoryStatus>('loading')
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setStatus('loading')
    setError(null)

    try {
      setInventory(await getImportedRecordingInventory())
      setStatus('ready')
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'The recording inventory could not be loaded.')
      setStatus('error')
    }
  }, [])

  useEffect(() => {
    let current = true
    void getImportedRecordingInventory()
      .then((response) => {
        if (current) {
          setInventory(response)
          setStatus('ready')
        }
      })
      .catch((caughtError) => {
        if (current) {
          setError(caughtError instanceof Error ? caughtError.message : 'The recording inventory could not be loaded.')
          setStatus('error')
        }
      })

    return () => {
      current = false
    }
  }, [])

  return { error, inventory, retry: () => void load(), status }
}

export { useImportedRecordingInventory }
