import { Upload } from 'lucide-react'
import './DropZone.scss'

const DropZone = () => {
  return (
    <div className="drop-zone">
      <div className="drop-zone__content">
        <div className="drop-zone__icon">
          <Upload size={48} />
        </div>
        <h2 className="drop-zone__title">Upload audio files</h2>
        <p className="drop-zone__subtitle">
          Drag and drop WAV files here, or click to browse
        </p>
        <p className="drop-zone__hint">
          Maximum 10 files • 50MB per file • WAV format only
        </p>
      </div>
    </div>
  )
}

export { DropZone }
