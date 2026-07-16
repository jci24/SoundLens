import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { RefObject, SyntheticEvent } from 'react'
import type { IAnalysisRegionOfInterest, ITimeWaveformRecording } from '../../types'
import { getPlaybackRecordingUrl } from '../services/playbackRecording'

type TPlaybackStatus = 'empty' | 'loading' | 'ready' | 'playing' | 'buffering' | 'error'

interface IPlaybackScope {
  endTimeSeconds: number
  hasRegionOfInterest: boolean
  startTimeSeconds: number
}

const unsupportedMediaSourceErrorCode = 4
const boundaryToleranceSeconds = 0.005
const interactiveTargetSelector = [
  'button',
  'input',
  'select',
  'textarea',
  '[contenteditable="true"]',
  '[role="dialog"]',
].join(',')

const getPlaybackScope = (
  recording: ITimeWaveformRecording | null,
  regionOfInterest: IAnalysisRegionOfInterest | null
): IPlaybackScope => {
  const durationSeconds = recording?.durationSeconds ?? 0

  if (!regionOfInterest || durationSeconds <= 0) {
    return {
      endTimeSeconds: durationSeconds,
      hasRegionOfInterest: false,
      startTimeSeconds: 0,
    }
  }

  const startTimeSeconds = Math.min(
    durationSeconds,
    Math.max(0, regionOfInterest.startTimeSeconds)
  )
  const endTimeSeconds = Math.min(
    durationSeconds,
    Math.max(startTimeSeconds, regionOfInterest.endTimeSeconds)
  )

  return {
    endTimeSeconds,
    hasRegionOfInterest: endTimeSeconds > startTimeSeconds,
    startTimeSeconds,
  }
}

const useRecordingPlayback = (
  recordings: ITimeWaveformRecording[],
  regionOfInterest: IAnalysisRegionOfInterest | null,
  workspaceRef: RefObject<HTMLElement | null>
) => {
  const audioRef = useRef<HTMLAudioElement | null>(null)
  const animationFrameRef = useRef<number | null>(null)
  const readyRecordingIdRef = useRef<string | null>(null)
  const previousRecordingIdRef = useRef<string | null>(null)
  const [selectedRecordingId, setSelectedRecordingId] = useState<string | null>(null)
  const [currentTimeSeconds, setCurrentTimeSeconds] = useState(0)
  const [status, setStatus] = useState<TPlaybackStatus>('empty')
  const [error, setError] = useState<string | null>(null)
  const [isLoopRequested, setIsLoopRequested] = useState(false)
  const selectedRecording = recordings.find(
    (recording) => recording.recordingId === selectedRecordingId
  ) ?? null
  const hasSelectedRecording = selectedRecording !== null
  const scope = useMemo(
    () => getPlaybackScope(selectedRecording, regionOfInterest),
    [regionOfInterest, selectedRecording]
  )
  const scopeRef = useRef(scope)
  const isLoopEnabled = scope.hasRegionOfInterest && isLoopRequested
  const isLoopEnabledRef = useRef(isLoopEnabled)

  useEffect(() => {
    scopeRef.current = scope
    isLoopEnabledRef.current = isLoopEnabled
  }, [isLoopEnabled, scope])

  const stopAnimation = useCallback(() => {
    if (animationFrameRef.current !== null) {
      cancelAnimationFrame(animationFrameRef.current)
      animationFrameRef.current = null
    }
  }, [])

  const updatePosition = useCallback((timeSeconds: number) => {
    setCurrentTimeSeconds(timeSeconds)
  }, [])

  const enforceBoundary = useCallback(() => {
    const audio = audioRef.current
    const activeScope = scopeRef.current

    if (!audio) {
      return false
    }

    if (audio.currentTime < activeScope.endTimeSeconds - boundaryToleranceSeconds) {
      updatePosition(audio.currentTime)
      return false
    }

    if (isLoopEnabledRef.current) {
      audio.currentTime = activeScope.startTimeSeconds
      updatePosition(activeScope.startTimeSeconds)
      return false
    }

    audio.pause()
    audio.currentTime = activeScope.endTimeSeconds
    updatePosition(activeScope.endTimeSeconds)
    return true
  }, [updatePosition])

  const startAnimation = useCallback(() => {
    stopAnimation()

    const tick = () => {
      if (enforceBoundary() || audioRef.current?.paused !== false) {
        animationFrameRef.current = null
        return
      }

      animationFrameRef.current = requestAnimationFrame(tick)
    }

    animationFrameRef.current = requestAnimationFrame(tick)
  }, [enforceBoundary, stopAnimation])

  useEffect(() => {
    const audio = audioRef.current
    const recordingChanged = previousRecordingIdRef.current !== selectedRecordingId
    previousRecordingIdRef.current = selectedRecordingId

    stopAnimation()
    audio?.pause()

    if (audio) {
      audio.currentTime = scope.startTimeSeconds
    }

    let isCancelled = false
    queueMicrotask(() => {
      if (isCancelled) {
        return
      }

      setCurrentTimeSeconds(scope.startTimeSeconds)
      setError(null)
      setStatus(
        hasSelectedRecording
          ? readyRecordingIdRef.current === selectedRecordingId
            ? 'ready'
            : recordingChanged
              ? 'loading'
              : 'ready'
          : 'empty'
      )

      if (!scope.hasRegionOfInterest) {
        setIsLoopRequested(false)
      }
    })

    return () => {
      isCancelled = true
    }
  }, [hasSelectedRecording, scope.endTimeSeconds, scope.hasRegionOfInterest, scope.startTimeSeconds, selectedRecordingId, stopAnimation])

  useEffect(() => {
    const audio = audioRef.current

    return () => {
      stopAnimation()
      audio?.pause()
      audio?.removeAttribute('src')
      audio?.load()
    }
  }, [stopAnimation])

  const resetPlayback = useCallback((nextTimeSeconds: number) => {
    const audio = audioRef.current
    stopAnimation()
    audio?.pause()

    if (audio) {
      audio.currentTime = nextTimeSeconds
    }

    updatePosition(nextTimeSeconds)
    setError(null)
  }, [stopAnimation, updatePosition])

  const selectRecording = useCallback((recordingId: string) => {
    const nextRecording = recordings.find((recording) => recording.recordingId === recordingId) ?? null
    const nextScope = getPlaybackScope(nextRecording, regionOfInterest)
    resetPlayback(nextScope.startTimeSeconds)
    readyRecordingIdRef.current = null
    setSelectedRecordingId(recordingId)
    setStatus('loading')
  }, [recordings, regionOfInterest, resetPlayback])

  const clearRecording = useCallback(() => {
    resetPlayback(0)
    readyRecordingIdRef.current = null
    setSelectedRecordingId(null)
    setStatus('empty')
    setIsLoopRequested(false)
  }, [resetPlayback])

  const togglePlayPause = useCallback(async () => {
    const audio = audioRef.current

    if (!audio || !selectedRecording) {
      return
    }

    if (!audio.paused) {
      audio.pause()
      return
    }

    const activeScope = scopeRef.current
    if (
      audio.currentTime < activeScope.startTimeSeconds ||
      audio.currentTime >= activeScope.endTimeSeconds - boundaryToleranceSeconds
    ) {
      audio.currentTime = activeScope.startTimeSeconds
      updatePosition(activeScope.startTimeSeconds)
    }

    setError(null)

    try {
      await audio.play()
    } catch {
      setStatus('error')
      setError('Playback could not start.')
    }
  }, [selectedRecording, updatePosition])

  const seek = useCallback((nextTimeSeconds: number) => {
    const audio = audioRef.current
    const activeScope = scopeRef.current
    const boundedTime = Math.min(
      Math.max(nextTimeSeconds, activeScope.startTimeSeconds),
      activeScope.endTimeSeconds
    )

    if (audio) {
      audio.currentTime = boundedTime
    }

    updatePosition(boundedTime)
  }, [updatePosition])

  const handleMediaError = useCallback(() => {
    stopAnimation()
    const errorCode = audioRef.current?.error?.code
    setStatus('error')
    setError(
      errorCode === unsupportedMediaSourceErrorCode
        ? 'This recording format is not supported by the browser.'
        : 'The recording could not be loaded.'
    )
  }, [stopAnimation])

  const handleEnded = useCallback(() => {
    const audio = audioRef.current

    if (audio && isLoopEnabledRef.current) {
      audio.currentTime = scopeRef.current.startTimeSeconds
      updatePosition(scopeRef.current.startTimeSeconds)
      void audio.play().catch(() => {
        setStatus('error')
        setError('Playback could not continue.')
      })
      return
    }

    stopAnimation()
    setStatus('ready')
  }, [stopAnimation, updatePosition])

  const handleWorkspaceKeyDown = useCallback((event: KeyboardEvent) => {
    if (event.code !== 'Space' || event.repeat) {
      return
    }

    const workspace = workspaceRef.current
    const activeElement = document.activeElement
    if (
      !workspace ||
      (activeElement !== document.body &&
        (!(activeElement instanceof HTMLElement) || !workspace.contains(activeElement)))
    ) {
      return
    }

    const interactionTarget = event.target instanceof HTMLElement ? event.target : activeElement
    if (interactionTarget instanceof HTMLElement && interactionTarget.closest(interactiveTargetSelector)) {
      return
    }

    event.preventDefault()
    void togglePlayPause()
  }, [togglePlayPause, workspaceRef])

  useEffect(() => {
    document.addEventListener('keydown', handleWorkspaceKeyDown)
    return () => document.removeEventListener('keydown', handleWorkspaceKeyDown)
  }, [handleWorkspaceKeyDown])

  return {
    audioRef: audioRef as RefObject<HTMLAudioElement>,
    clearRecording,
    currentTimeSeconds,
    error,
    handleCanPlay: (event: SyntheticEvent<HTMLAudioElement>) => {
      setStatus(event.currentTarget.paused ? 'ready' : 'playing')
    },
    handleEnded,
    handleLoadedMetadata: () => {
      const audio = audioRef.current
      readyRecordingIdRef.current = selectedRecordingId
      if (audio) {
        audio.currentTime = scopeRef.current.startTimeSeconds
      }
      updatePosition(scopeRef.current.startTimeSeconds)
      setStatus('ready')
    },
    handleLoadStart: () => setStatus('loading'),
    handleMediaError,
    handlePause: () => {
      stopAnimation()
      setStatus((current) => current === 'error' || current === 'empty' ? current : 'ready')
    },
    handlePlaying: () => {
      setStatus('playing')
      startAnimation()
    },
    handleTimeUpdate: enforceBoundary,
    handleWaiting: () => {
      stopAnimation()
      setStatus('buffering')
    },
    isLoopEnabled,
    isPlaying: status === 'playing',
    playbackUrl: selectedRecording
      ? getPlaybackRecordingUrl(selectedRecording.recordingId)
      : undefined,
    scope,
    seek,
    selectRecording,
    selectedRecording,
    selectedRecordingId: selectedRecording?.recordingId ?? null,
    status,
    toggleLoop: () => setIsLoopRequested((current) => !current),
    togglePlayPause,
  }
}

export { getPlaybackScope, useRecordingPlayback }
export type { TPlaybackStatus }
