import { useEffect, useRef, useState } from 'react'
import { Headphones, Pause, Play, X } from 'lucide-react'
import type { ITimeWaveformRecording, TComparisonGroupAssignment } from '../../types'
import { getPlaybackRecordingUrl } from '../services/playbackRecording'
import { PlaybackRecordingPicker } from './PlaybackRecordingPicker'
import './AudioTransport.scss'

interface IAudioTransportProps {
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
}

type TPlaybackStatus = 'empty' | 'loading' | 'ready' | 'playing' | 'buffering' | 'error'

const unsupportedMediaSourceErrorCode = 4

const formatPlaybackTime = (seconds: number) => {
  if (!Number.isFinite(seconds) || seconds < 0) {
    return '0:00'
  }

  const wholeSeconds = Math.floor(seconds)
  const minutes = Math.floor(wholeSeconds / 60)
  return `${minutes}:${String(wholeSeconds % 60).padStart(2, '0')}`
}

const AudioTransport = ({ recordings, recordingGroupAssignments }: IAudioTransportProps) => {
  const audioRef = useRef<HTMLAudioElement | null>(null)
  const [selectedRecordingId, setSelectedRecordingId] = useState<string | null>(null)
  const [currentTimeSeconds, setCurrentTimeSeconds] = useState(0)
  const [status, setStatus] = useState<TPlaybackStatus>('empty')
  const [error, setError] = useState<string | null>(null)
  const selectedRecording = recordings.find(
    (recording) => recording.recordingId === selectedRecordingId
  ) ?? null
  const playbackDuration = selectedRecording?.durationSeconds ?? 0
  const playbackUrl = selectedRecording
    ? getPlaybackRecordingUrl(selectedRecording.recordingId)
    : undefined
  const isPlaying = status === 'playing'

  useEffect(() => {
    const audio = audioRef.current

    return () => {
      audio?.pause()
      audio?.removeAttribute('src')
      audio?.load()
    }
  }, [])

  const resetPlayback = () => {
    const audio = audioRef.current
    audio?.pause()

    if (audio) {
      audio.currentTime = 0
    }

    setCurrentTimeSeconds(0)
    setError(null)
  }

  const handleRecordingSelect = (recordingId: string) => {
    resetPlayback()
    setSelectedRecordingId(recordingId)
    setStatus('loading')
  }

  const handleClear = () => {
    resetPlayback()
    setSelectedRecordingId(null)
    setStatus('empty')
  }

  const handlePlayPause = async () => {
    const audio = audioRef.current

    if (!audio || !selectedRecording) {
      return
    }

    if (!audio.paused) {
      audio.pause()
      return
    }

    setError(null)

    try {
      await audio.play()
    } catch {
      setStatus('error')
      setError('Playback could not start.')
    }
  }

  const handleSeek = (nextTimeSeconds: number) => {
    const audio = audioRef.current
    const boundedTime = Math.min(Math.max(nextTimeSeconds, 0), playbackDuration)

    if (audio) {
      audio.currentTime = boundedTime
    }

    setCurrentTimeSeconds(boundedTime)
  }

  const handleMediaError = () => {
    const errorCode = audioRef.current?.error?.code
    setStatus('error')
    setError(
      errorCode === unsupportedMediaSourceErrorCode
        ? 'This recording format is not supported by the browser.'
        : 'The recording could not be loaded.'
    )
  }

  return (
    <section className="audio-transport" aria-label="Recording playback">
      <audio
        aria-hidden="true"
        onCanPlay={(event) => setStatus(event.currentTarget.paused ? 'ready' : 'playing')}
        onEnded={() => setStatus('ready')}
        onError={handleMediaError}
        onLoadStart={() => setStatus('loading')}
        onLoadedMetadata={() => setStatus('ready')}
        onPause={() => setStatus((current) => current === 'error' || current === 'empty' ? current : 'ready')}
        onPlaying={() => setStatus('playing')}
        onTimeUpdate={(event) => setCurrentTimeSeconds(event.currentTarget.currentTime)}
        onWaiting={() => setStatus('buffering')}
        preload="metadata"
        ref={audioRef}
        src={playbackUrl}
      />

      <div className="audio-transport__identity">
        <Headphones aria-hidden="true" size={15} />
        <span>Playback</span>
      </div>

      <PlaybackRecordingPicker
        onSelect={handleRecordingSelect}
        recordings={recordings}
        recordingGroupAssignments={recordingGroupAssignments}
        selectedRecording={selectedRecording}
      />

      {selectedRecording && (
        <button
          aria-label="Clear playback recording"
          className="audio-transport__clear"
          type="button"
          onClick={handleClear}
        >
          <X aria-hidden="true" size={14} />
        </button>
      )}

      <button
        aria-label={isPlaying ? 'Pause recording' : 'Play recording'}
        className="audio-transport__play"
        disabled={!selectedRecording || status === 'loading'}
        type="button"
        onClick={() => void handlePlayPause()}
      >
        {isPlaying ? <Pause aria-hidden="true" size={15} /> : <Play aria-hidden="true" size={15} />}
      </button>

      <input
        aria-label="Playback position"
        className="audio-transport__seek"
        disabled={!selectedRecording}
        max={playbackDuration}
        min={0}
        onChange={(event) => handleSeek(Number(event.target.value))}
        step="0.01"
        type="range"
        value={Math.min(currentTimeSeconds, playbackDuration)}
      />

      <span className="audio-transport__time">
        {formatPlaybackTime(currentTimeSeconds)} / {formatPlaybackTime(playbackDuration)}
      </span>

      <span className={`audio-transport__status audio-transport__status--${status}`} aria-live="polite">
        {error ?? (status === 'loading' ? 'Loading' : status === 'buffering' ? 'Buffering' : '')}
      </span>
    </section>
  )
}

export { AudioTransport }
