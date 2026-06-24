import { useState } from 'react'
import { toast } from 'sonner'
import { importFiles } from '../services/importFiles'
import type { IImportRequest } from '../types'

export interface IUseImportFiles {
  handleImportFiles: (request: IImportRequest) => Promise<void>
  isImporting: boolean
}

export const useImportFiles = (): IUseImportFiles => {
  const [isImporting, setIsImporting] = useState(false)

  const handleImportFiles = async (request: IImportRequest): Promise<void> => {
    if (request.filePaths.length === 0) return

    setIsImporting(true)

    try {
      const response = await importFiles(request)
      const importedCount = response.succeededFiles.length
      const failedCount = response.failedFiles.length

      if (importedCount > 0) {
        toast.success(
          `${importedCount} file${importedCount === 1 ? '' : 's'} imported successfully`
        )
      }

      if (failedCount > 0) {
        toast.error(
          `${failedCount} file${failedCount === 1 ? '' : 's'} failed to import`
        )
      }
    } catch (caughtError) {
      toast.error(caughtError instanceof Error ? caughtError.message : 'Import failed.')
    } finally {
      setIsImporting(false)
    }
  }

  return {
    handleImportFiles,
    isImporting,
  }
}
