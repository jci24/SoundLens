import { useCallback, useEffect, useState } from 'react'
import type {
  IImportedFileResult,
  IImportSessionFileSummary,
} from '../../../common/contracts/import'
import { getCurrentImportSession } from '../services/currentImportSession'

type TImportSessionStatus = 'loading' | 'ready' | 'error'

const toSessionFile = ({ fileName, sizeBytes, contentType }: IImportedFileResult): IImportSessionFileSummary => ({
  fileName,
  sizeBytes,
  contentType,
})

const useCurrentImportSession = () => {
  const [files, setFiles] = useState<IImportSessionFileSummary[]>([])
  const [status, setStatus] = useState<TImportSessionStatus>('loading')
  const [error, setError] = useState<string | null>(null)

  const restoreSession = useCallback(async () => {
    setStatus('loading')
    setError(null)

    try {
      const response = await getCurrentImportSession()
      setFiles(response.files)
      setStatus('ready')
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'The current import session could not be restored.')
      setStatus('error')
    }
  }, [])

  useEffect(() => {
    let current = true
    void getCurrentImportSession()
      .then((response) => {
        if (current) {
          setFiles(response.files)
          setStatus('ready')
        }
      })
      .catch((caughtError) => {
        if (current) {
          setError(caughtError instanceof Error ? caughtError.message : 'The current import session could not be restored.')
          setStatus('error')
        }
      })

    return () => {
      current = false
    }
  }, [])

  const acceptImportedFiles = useCallback((importedFiles: IImportedFileResult[]) => {
    setFiles(importedFiles.map(toSessionFile))
    setError(null)
    setStatus('ready')
  }, [])

  return {
    acceptImportedFiles,
    error,
    files,
    retry: () => void restoreSession(),
    status,
  }
}

export { useCurrentImportSession }
export type { TImportSessionStatus }
