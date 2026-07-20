import { useState } from 'react'
import { toast } from 'sonner'
import { uploadFiles } from '../services/importFiles'
import type { IImportFilesResponse } from '../../../common/contracts/import'

export interface IUseImportFiles {
  importError: string | null
  handleUploadFiles: (files: File[]) => Promise<IImportFilesResponse | undefined>
  isImporting: boolean
}

export const useImportFiles = (): IUseImportFiles => {
  const [isImporting, setIsImporting] = useState(false)
  const [importError, setImportError] = useState<string | null>(null)

  const handleImport = async (
    runImport: () => Promise<IImportFilesResponse>
  ): Promise<IImportFilesResponse | undefined> => {
    setIsImporting(true)
    setImportError(null)

    try {
      const response = await runImport()
      const importedCount = response.succeededFiles.length
      const failedCount = response.failedFiles.length

      if (importedCount > 0) {
        toast.success(
          `${importedCount} file${importedCount === 1 ? '' : 's'} imported successfully`
        )
      }

      if (failedCount > 0) {
        setImportError(`${failedCount} file${failedCount === 1 ? '' : 's'} could not be imported.`)
        toast.error(
          `${failedCount} file${failedCount === 1 ? '' : 's'} failed to import`
        )
      }

      return response
    } catch (caughtError) {
      const message = caughtError instanceof Error ? caughtError.message : 'Import failed.'
      setImportError(message)
      toast.error(message)
      return undefined
    } finally {
      setIsImporting(false)
    }
  }

  const handleUploadFiles = async (files: File[]): Promise<IImportFilesResponse | undefined> => {
    if (files.length === 0) return undefined

    return handleImport(() => uploadFiles(files))
  }

  return {
    handleUploadFiles,
    importError,
    isImporting,
  }
}
