import { useRef } from 'react'
import type { ChangeEvent } from 'react'
import { Upload } from 'lucide-react'
import './FilePickerImporter.scss'

interface IFilePickerImporterProps {
  onImport: (files: File[]) => void
  disabled: boolean
}

const acceptedFileTypes = '.wav,.mp3,.aiff,.aif,.flac,.ogg,audio/*'

const FilePickerImporter = ({ onImport, disabled }: IFilePickerImporterProps) => {
  const inputRef = useRef<HTMLInputElement | null>(null)

  const handleSelection = (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])

    if (files.length > 0) {
      onImport(files)
    }

    event.target.value = ''
  }

  return (
    <div className="file-picker-importer">
      <input
        ref={inputRef}
        className="file-picker-importer__input"
        type="file"
        accept={acceptedFileTypes}
        multiple
        onChange={handleSelection}
        disabled={disabled}
      />
      <button
        className="file-picker-importer__target"
        type="button"
        onClick={() => inputRef.current?.click()}
        disabled={disabled}
      >
        <div className="file-picker-importer__icon" aria-hidden="true">
          <Upload size={24} />
        </div>
        <div className="file-picker-importer__copy">
          <p className="file-picker-importer__title">Pick audio files</p>
          <p className="file-picker-importer__subtitle">
            Choose WAV, MP3, AIFF, FLAC, or OGG files from your computer.
          </p>
        </div>
        </button>
    </div>
  )
}

export { FilePickerImporter }
