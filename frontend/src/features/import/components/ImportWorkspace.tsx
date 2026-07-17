import { AlertCircle, Loader2 } from 'lucide-react'
import { useImportFiles } from '../hooks/useImportFiles'
import type { IImportedFileSummary } from '../../../common/contracts/import'
import { FilePickerImporter } from './FilePickerImporter/FilePickerImporter'
import './ImportWorkspace.scss'

interface IImportWorkspaceProps {
  onImportedFiles: (files: IImportedFileSummary[]) => void
}

const ImportWorkspace = ({ onImportedFiles }: IImportWorkspaceProps) => {
  const { handleUploadFiles, importError, isImporting } = useImportFiles()

  const handleFiles = (files: File[]) => {
    void handleUploadFiles(files).then((response) => {
      if (response && response.succeededFiles.length > 0) {
        onImportedFiles(response.succeededFiles)
      }
    })
  }

  if (isImporting) {
    return (
      <section className="import-workspace" aria-label="Importing audio files">
        <div className="import-workspace__loading">
          <Loader2 className="import-workspace__spinner" size={28} />
          <span>Importing files…</span>
        </div>
      </section>
    )
  }

  return (
    <section className="import-workspace" aria-label="Import audio files">
      <FilePickerImporter onImport={handleFiles} disabled={isImporting} />
      {importError && (
        <div className="import-workspace__error" role="alert">
          <AlertCircle aria-hidden="true" size={16} />
          <span>{importError}</span>
        </div>
      )}
    </section>
  )
}

export { ImportWorkspace }
