import { useState } from 'react'
import { importFiles } from '../services/importFiles'
import type { IImportFilesResponse, IImportRequest } from '../types'

export interface IUseImportFiles {
  error: string | null
  handleImportFiles: (request: IImportRequest) => Promise<void>
  isImporting: boolean
  result: IImportFilesResponse | null
}

export const useImportFiles = (): IUseImportFiles => {
  const [result, setResult] = useState<IImportFilesResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isImporting, setIsImporting] = useState(false)

  const handleImportFiles = async (request: IImportRequest): Promise<void> => {
    setError(null)

    if (request.filePaths.length === 0) return

    setIsImporting(true)

    try {
      const response = await importFiles(request)
      setResult(response)
    } catch (caughtError) {
      setResult(null)
      setError(caughtError instanceof Error ? caughtError.message : 'Import failed.')
    } finally {
      setIsImporting(false)
    }
  }

  return {
    error,
    handleImportFiles,
    isImporting,
    result,
  }
}
