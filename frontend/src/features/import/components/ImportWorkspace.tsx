import { CheckCircle2, FileAudio2, Loader2 } from 'lucide-react'
import { useImportFiles } from '../hooks/useImportFiles'
import { isInWebView } from '../utils/webview'
import { TextBasedImporter } from './TextBasedImporter/TextBasedImporter'
import './ImportWorkspace.scss'

const ImportWorkspace = () => {
  const { error, handleImportFiles, isImporting, result } = useImportFiles()
  const inWebView = isInWebView()

  const handlePaths = (paths: string[]) => {
    void handleImportFiles({ filePaths: paths })
  }

  const importedCount = result?.succeededFiles.length ?? 0

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

      {error && (
        <p className="import-workspace__error" role="alert">
          {error}
        </p>
      )}

      {result && (
        <div className="import-workspace__result" role="status">
          <div className="import-workspace__result-heading">
            <CheckCircle2 size={20} aria-hidden="true" />
            <h1>{importedCount} file{importedCount === 1 ? '' : 's'} imported</h1>
          </div>

          <ul className="import-workspace__file-list" aria-label="Imported files">
            {result.succeededFiles.map((file) => (
              <li key={`${file.fileName}-${file.sizeBytes}`}>
                <FileAudio2 size={18} aria-hidden="true" />
                <span>{file.fileName}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  )
}

export { ImportWorkspace }
