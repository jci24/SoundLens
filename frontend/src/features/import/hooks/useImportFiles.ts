import { useState } from 'react'
import { toast } from 'sonner'
import { importFilesByPath, uploadFiles } from '../services/importFiles'

export interface IUseImportFiles {
  handleImportPaths: (filePaths: string[]) => Promise<void>
  handleUploadFiles: (files: File[]) => Promise<void>
  isImporting: boolean
}

export const useImportFiles = (): IUseImportFiles => {
  const [isImporting, setIsImporting] = useState(false)

  const handleImport = async (runImport: () => Promise<Awaited<ReturnType<typeof importFilesByPath>>>) => {
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
    } catch (caughtError) {
      toast.error(caughtError instanceof Error ? caughtError.message : 'Import failed.')
    } finally {
      setIsImporting(false)
    }
  }

  const handleImportPaths = async (filePaths: string[]): Promise<void> => {
    if (filePaths.length === 0) return

    await handleImport(() => importFilesByPath({ filePaths }))
  }

  const handleUploadFiles = async (files: File[]): Promise<void> => {
    if (files.length === 0) return

    await handleImport(() => uploadFiles(files))
  }

  return {
    handleImportPaths,
    handleUploadFiles,
    isImporting,
  }
}
