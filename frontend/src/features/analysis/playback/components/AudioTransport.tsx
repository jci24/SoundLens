import { Headphones, Pause, Play, Repeat2, X } from 'lucide-react'
import { PlaybackRecordingPicker } from './PlaybackRecordingPicker'
import { AbAuditionToggle } from './AbAuditionToggle'
import { useRecordingPlaybackContext } from '../contexts/recordingPlaybackContext'
import './AudioTransport.scss'

const formatPlaybackTime = (seconds: number) => {
  if (!Number.isFinite(seconds) || seconds < 0) {
    return '0:00'
  }

  const wholeSeconds = Math.floor(seconds)
  const minutes = Math.floor(wholeSeconds / 60)
  return `${minutes}:${String(wholeSeconds % 60).padStart(2, '0')}`
}

const AudioTransport = () => {
  const {
    activePairSide,
    audioRef,
    auditionPair,
    clearRecording,
    currentTimeSeconds,
    error,
    handleCanPlay,
    handleEnded,
    handleLoadedMetadata,
    handleLoadStart,
    handleMediaError,
    handlePause,
    handlePlaying,
    handleTimeUpdate,
    handleWaiting,
    isLoopEnabled,
    isPlaying,
    playbackUrl,
    recordings,
    recordingGroupAssignments,
    scope,
    seek,
    secondaryAudioRef,
    secondaryPlaybackUrl,
    selectAuditionSide,
    selectRecording,
    selectedRecording,
    status,
    toggleLoop,
    togglePlayPause,
  } = useRecordingPlaybackContext()

  return (
    <section className="audio-transport" aria-label="Recording playback">
      <audio
        aria-hidden="true"
        onCanPlay={handleCanPlay}
        onEnded={handleEnded}
        onError={handleMediaError}
        onLoadStart={handleLoadStart}
        onLoadedMetadata={handleLoadedMetadata}
        onPause={handlePause}
        onPlaying={handlePlaying}
        onTimeUpdate={handleTimeUpdate}
        onWaiting={handleWaiting}
        preload="metadata"
        ref={audioRef}
        src={playbackUrl}
      />
      {secondaryPlaybackUrl && (
        <audio
          aria-hidden="true"
          key={secondaryPlaybackUrl}
          preload="metadata"
          ref={secondaryAudioRef}
          src={secondaryPlaybackUrl}
        />
      )}

      <div className="audio-transport__identity">
        <Headphones aria-hidden="true" size={15} />
        <span>Playback</span>
      </div>

      <PlaybackRecordingPicker
        onSelect={selectRecording}
        recordings={recordings}
        recordingGroupAssignments={recordingGroupAssignments}
        selectedRecording={selectedRecording}
      />

      {auditionPair && (
        <AbAuditionToggle
          activeSide={activePairSide}
          onSelect={selectAuditionSide}
          pair={auditionPair}
        />
      )}

      {selectedRecording && (
        <button
          aria-label="Clear playback recording"
          className="audio-transport__clear"
          type="button"
          onClick={clearRecording}
        >
          <X aria-hidden="true" size={14} />
        </button>
      )}

      <button
        aria-label={isPlaying ? 'Pause recording' : 'Play recording'}
        className="audio-transport__play"
        disabled={!selectedRecording || status === 'loading'}
        type="button"
        onClick={() => void togglePlayPause()}
      >
        {isPlaying ? <Pause aria-hidden="true" size={15} /> : <Play aria-hidden="true" size={15} />}
      </button>

      <input
        aria-label="Playback position"
        className="audio-transport__seek"
        disabled={!selectedRecording}
        max={scope.endTimeSeconds}
        min={scope.startTimeSeconds}
        onChange={(event) => seek(Number(event.target.value))}
        step="0.01"
        type="range"
        value={Math.min(Math.max(currentTimeSeconds, scope.startTimeSeconds), scope.endTimeSeconds)}
      />

      <span className="audio-transport__time">
        {formatPlaybackTime(currentTimeSeconds)} / {formatPlaybackTime(scope.endTimeSeconds)}
      </span>

      <button
        aria-label="Loop selected region"
        aria-pressed={isLoopEnabled}
        className="audio-transport__loop"
        disabled={!scope.hasRegionOfInterest || !selectedRecording}
        title={scope.hasRegionOfInterest ? 'Loop selected region' : 'Select a region to enable looping'}
        type="button"
        onClick={toggleLoop}
      >
        <Repeat2 aria-hidden="true" size={14} />
      </button>

      <span className={`audio-transport__status audio-transport__status--${status}`} aria-live="polite">
        {error ?? (
          status === 'loading'
            ? `Loading${activePairSide ? ` ${activePairSide}` : ''}`
            : status === 'buffering'
              ? `Buffering${activePairSide ? ` ${activePairSide}` : ''}`
              : ''
        )}
      </span>
    </section>
  )
}

export { AudioTransport }
