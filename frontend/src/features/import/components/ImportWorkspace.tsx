import { Loader2 } from 'lucide-react'
import { useImportFiles } from '../hooks/useImportFiles'
import { isInWebView } from '../utils/webview'
import { TextBasedImporter } from './TextBasedImporter/TextBasedImporter'
import './ImportWorkspace.scss'

const ImportWorkspace = () => {
  const { handleImportFiles, isImporting } = useImportFiles()
  const inWebView = isInWebView()

  const handlePaths = (paths: string[]) => {
    void handleImportFiles({ filePaths: paths })
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
      {inWebView ? (
        <div className="import-workspace__webview-mode">
          <p className="import-workspace__webview-placeholder">
            Native file picker will be available when running inside the desktop webview.
          </p>
        </div>
      ) : (
        <TextBasedImporter onImport={handlePaths} disabled={isImporting} />
      )}
    </section>
  )
}

export { ImportWorkspace }
