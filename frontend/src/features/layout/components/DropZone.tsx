import { Upload, CheckCircle, AlertCircle, Loader2 } from 'lucide-react'
import { useUpload } from '../../upload/hooks/useUpload'
import './DropZone.scss'

const DropZone = () => {
  const upload = useUpload()

  const handleFileSelect = (files: FileList | null) => {
    if (!files || files.length === 0) return

    const file = files[0]
    if (!file.name.toLowerCase().endsWith('.wav')) {
      alert('Only WAV files are supported')
      return
    }

    upload.mutate(file)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    handleFileSelect(e.dataTransfer.files)
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
  }

  const handleClick = () => {
    const input = document.createElement('input')
    input.type = 'file'
    input.accept = '.wav'
    input.onchange = (e) => handleFileSelect((e.target as HTMLInputElement).files)
    input.click()
  }

  return (
    <div 
      className="drop-zone"
      onDrop={handleDrop}
      onDragOver={handleDragOver}
      onClick={handleClick}
    >
      <div className="drop-zone__content">
        <div className="drop-zone__icon">
          {upload.isPending ? (
            <Loader2 size={48} className="animate-spin" />
          ) : upload.isSuccess ? (
            <CheckCircle size={48} />
          ) : upload.isError ? (
            <AlertCircle size={48} />
          ) : (
            <Upload size={48} />
          )}
        </div>
        <h2 className="drop-zone__title">
          {upload.isPending ? 'Uploading...' : 
           upload.isSuccess ? 'Upload successful!' :
           upload.isError ? 'Upload failed' :
           'Upload audio files'}
        </h2>
        <p className="drop-zone__subtitle">
          {upload.isError ? (upload.error as Error).message :
           'Drag and drop WAV files here, or click to browse'}
        </p>
        {upload.isSuccess && upload.data && (
          <div className="drop-zone__metadata">
            <p>Sample Rate: {upload.data.metadata?.sampleRate} Hz</p>
            <p>Duration: {upload.data.metadata?.durationSeconds.toFixed(2)}s</p>
            <p>Channels: {upload.data.metadata?.channels}</p>
          </div>
        )}
        {!upload.isSuccess && (
          <p className="drop-zone__hint">
            Maximum 10 files • 50MB per file • WAV format only
          </p>
        )}
      </div>
    </div>
  )
}

export { DropZone }
