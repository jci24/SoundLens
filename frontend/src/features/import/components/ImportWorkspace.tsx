import { Loader2 } from 'lucide-react'
import { useImportFiles } from '../hooks/useImportFiles'
import { FilePickerImporter } from './FilePickerImporter/FilePickerImporter'
import './ImportWorkspace.scss'

const ImportWorkspace = () => {
  const { handleUploadFiles, isImporting } = useImportFiles()

  const handleFiles = (files: File[]) => {
    void handleUploadFiles(files)
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
    </section>
  )
}

export { ImportWorkspace }
