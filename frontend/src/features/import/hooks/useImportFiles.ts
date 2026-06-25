import { useState } from 'react'
import { toast } from 'sonner'
import { importFilesByPath, uploadFiles } from '../services/importFiles'
import type { IImportFilesResponse } from '../../../common/contracts/import'

export interface IUseImportFiles {
  handleImportPaths: (filePaths: string[]) => Promise<IImportFilesResponse | undefined>
  handleUploadFiles: (files: File[]) => Promise<IImportFilesResponse | undefined>
  isImporting: boolean
}

export const useImportFiles = (): IUseImportFiles => {
  const [isImporting, setIsImporting] = useState(false)

  const handleImport = async (
    runImport: () => Promise<IImportFilesResponse>
  ): Promise<IImportFilesResponse | undefined> => {
    setIsImporting(true)

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
        toast.error(
          `${failedCount} file${failedCount === 1 ? '' : 's'} failed to import`
        )
      }

      return response
    } catch (caughtError) {
      toast.error(caughtError instanceof Error ? caughtError.message : 'Import failed.')
      return undefined
    } finally {
      setIsImporting(false)
    }
  }

  const handleImportPaths = async (filePaths: string[]): Promise<IImportFilesResponse | undefined> => {
    if (filePaths.length === 0) return undefined

    return handleImport(() => importFilesByPath({ filePaths }))
  }

  const handleUploadFiles = async (files: File[]): Promise<IImportFilesResponse | undefined> => {
    if (files.length === 0) return undefined

    return handleImport(() => uploadFiles(files))
  }

  return {
    handleImportPaths,
    handleUploadFiles,
    isImporting,
  }
}
