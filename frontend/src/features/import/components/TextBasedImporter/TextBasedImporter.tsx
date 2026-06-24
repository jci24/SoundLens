import { useState } from 'react'
import { FileAudio2, Upload } from 'lucide-react'
import './TextBasedImporter.scss'

interface ITextBasedImporterProps {
  onImport: (filePaths: string[]) => void
  disabled: boolean
}

const TextBasedImporter = ({ onImport, disabled }: ITextBasedImporterProps) => {
  const [input, setInput] = useState('')

  const handleImport = () => {
    const PLACEHOLDER = '§§SPACE§§'
    const paths = input
      .replace(/\\ /g, PLACEHOLDER)
      .split(/[\n\s]+/)
      .map((path) => path.replaceAll(PLACEHOLDER, ' ').replace(/\\(.)/g, '$1').trim())
      .filter((path) => path.length > 0)

    if (paths.length === 0) return

    onImport(paths)
  }

  return (
    <div className="text-importer">
      <div className="text-importer__dropzone">
        <div className="text-importer__icon-wrapper">
          <Upload size={20} aria-hidden="true" />
        </div>
        <div className="text-importer__copy">
          <p className="text-importer__title">Import audio files by path</p>
          <p className="text-importer__subtitle">Paste file paths below — WAV, MP3, AIFF, FLAC, etc.</p>
        </div>
        <textarea
          className="text-importer__textarea"
          placeholder={'/Users/jaime/Music/track-1.wav\n/Users/jaime/Music/track-2.wav'}
          rows={5}
          value={input}
          onChange={(event) => setInput(event.target.value)}
          disabled={disabled}
          spellCheck={false}
        />
        <button
          className="text-importer__button"
          type="button"
          onClick={handleImport}
          disabled={disabled || input.trim().length === 0}
        >
          <FileAudio2 size={18} aria-hidden="true" />
          Import files
        </button>
      </div>
    </div>
  )
}

export { TextBasedImporter }
