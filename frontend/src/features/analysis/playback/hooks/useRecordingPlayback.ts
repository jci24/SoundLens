import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { RefObject, SyntheticEvent } from 'react'
import type {
  IAnalysisRegionOfInterest,
  ITimeWaveformRecording,
  TComparisonGroupAssignment,
} from '../../types'
import { getPlaybackRecordingUrl } from '../services/playbackRecording'

type TPlaybackStatus = 'empty' | 'loading' | 'ready' | 'playing' | 'buffering' | 'error'

interface IPlaybackScope {
  endTimeSeconds: number
  hasRegionOfInterest: boolean
  startTimeSeconds: number
}

interface IPendingAuditionSwitch {
  recordingId: string
  shouldResume: boolean
  timeSeconds: number
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

const getPositionAlignedTime = (
  timeSeconds: number,
  recording: ITimeWaveformRecording,
  regionOfInterest: IAnalysisRegionOfInterest | null
) => {
  const scope = getPlaybackScope(recording, regionOfInterest)
  return Math.min(Math.max(timeSeconds, scope.startTimeSeconds), scope.endTimeSeconds)
}

const useRecordingPlayback = (
  recordings: ITimeWaveformRecording[],
  regionOfInterest: IAnalysisRegionOfInterest | null,
  workspaceRef: RefObject<HTMLElement | null>,
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment> = {},
  isCompareMode = false
) => {
  const audioRef = useRef<HTMLAudioElement | null>(null)
  const secondaryAudioRef = useRef<HTMLAudioElement | null>(null)
  const animationFrameRef = useRef<number | null>(null)
  const readyRecordingIdRef = useRef<string | null>(null)
  const pendingAuditionSwitchRef = useRef<IPendingAuditionSwitch | null>(null)
  const previousRecordingIdRef = useRef<string | null>(null)
  const [selectedRecordingId, setSelectedRecordingId] = useState<string | null>(null)
  const [currentTimeSeconds, setCurrentTimeSeconds] = useState(0)
  const [status, setStatus] = useState<TPlaybackStatus>('empty')
  const [error, setError] = useState<string | null>(null)
  const [isLoopRequested, setIsLoopRequested] = useState(false)
  const selectedRecording = recordings.find(
    (recording) => recording.recordingId === selectedRecordingId
  ) ?? null
  const auditionPair = useMemo(() => {
    if (!isCompareMode) {
      return null
    }

    const pairRecordingsA = recordings.filter(
      (recording) => recordingGroupAssignments[recording.recordingId] === 'A'
    )
    const pairRecordingsB = recordings.filter(
      (recording) => recordingGroupAssignments[recording.recordingId] === 'B'
    )

    return pairRecordingsA.length === 1 && pairRecordingsB.length === 1
      ? { A: pairRecordingsA[0], B: pairRecordingsB[0] }
      : null
  }, [isCompareMode, recordingGroupAssignments, recordings])
  const activePairSide: 'A' | 'B' | null = auditionPair?.A.recordingId === selectedRecordingId
    ? 'A'
    : auditionPair?.B.recordingId === selectedRecordingId
      ? 'B'
      : null
  const inactivePairRecording = activePairSide === 'A'
    ? auditionPair?.B ?? null
    : activePairSide === 'B'
      ? auditionPair?.A ?? null
      : null
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
    const pendingSwitch = pendingAuditionSwitchRef.current?.recordingId === selectedRecordingId
      ? pendingAuditionSwitchRef.current
      : null
    previousRecordingIdRef.current = selectedRecordingId

    stopAnimation()
    audio?.pause()

    const nextTimeSeconds = pendingSwitch?.timeSeconds ?? scope.startTimeSeconds
    if (audio && !recordingChanged) {
      audio.currentTime = nextTimeSeconds
    }

    let isCancelled = false
    queueMicrotask(() => {
      if (isCancelled) {
        return
      }

      setCurrentTimeSeconds(nextTimeSeconds)
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

  useEffect(() => {
    const secondaryAudio = secondaryAudioRef.current

    return () => {
      secondaryAudio?.pause()
      secondaryAudio?.removeAttribute('src')
      secondaryAudio?.load()
    }
  }, [inactivePairRecording?.recordingId])

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
    pendingAuditionSwitchRef.current = null
    readyRecordingIdRef.current = null
    setSelectedRecordingId(recordingId)
    setStatus('loading')
  }, [recordings, regionOfInterest, resetPlayback])

  const clearRecording = useCallback(() => {
    resetPlayback(0)
    pendingAuditionSwitchRef.current = null
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

  const selectAuditionSide = useCallback((side: 'A' | 'B') => {
    if (!auditionPair) {
      return
    }

    const targetRecording = auditionPair[side]
    if (targetRecording.recordingId === selectedRecordingId) {
      return
    }

    if (!activePairSide) {
      selectRecording(targetRecording.recordingId)
      return
    }

    const audio = audioRef.current
    const nextTimeSeconds = getPositionAlignedTime(
      audio?.currentTime ?? scope.startTimeSeconds,
      targetRecording,
      regionOfInterest
    )
    pendingAuditionSwitchRef.current = {
      recordingId: targetRecording.recordingId,
      shouldResume: audio?.paused === false,
      timeSeconds: nextTimeSeconds,
    }
    readyRecordingIdRef.current = null
    stopAnimation()
    audio?.pause()
    setCurrentTimeSeconds(nextTimeSeconds)
    setError(null)
    setStatus('loading')
    setSelectedRecordingId(targetRecording.recordingId)
  }, [activePairSide, auditionPair, regionOfInterest, scope.startTimeSeconds, selectRecording, selectedRecordingId, stopAnimation])

  return {
    activePairSide,
    audioRef: audioRef as RefObject<HTMLAudioElement>,
    auditionPair,
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
      const pendingSwitch = pendingAuditionSwitchRef.current?.recordingId === selectedRecordingId
        ? pendingAuditionSwitchRef.current
        : null
      const requestedTimeSeconds = pendingSwitch?.timeSeconds ?? scopeRef.current.startTimeSeconds
      const nextTimeSeconds = Math.min(
        Math.max(requestedTimeSeconds, scopeRef.current.startTimeSeconds),
        scopeRef.current.endTimeSeconds
      )
      if (audio) {
        audio.currentTime = nextTimeSeconds
      }
      updatePosition(nextTimeSeconds)
      setStatus('ready')

      if (pendingSwitch?.shouldResume && audio) {
        pendingAuditionSwitchRef.current = null
        void audio.play().catch(() => {
          setStatus('error')
          setError('Playback could not continue after switching.')
        })
      } else {
        pendingAuditionSwitchRef.current = null
      }
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
    secondaryAudioRef: secondaryAudioRef as RefObject<HTMLAudioElement>,
    secondaryPlaybackUrl: inactivePairRecording
      ? getPlaybackRecordingUrl(inactivePairRecording.recordingId)
      : undefined,
    scope,
    seek,
    selectRecording,
    selectAuditionSide,
    selectedRecording,
    selectedRecordingId: selectedRecording?.recordingId ?? null,
    status,
    toggleLoop: () => setIsLoopRequested((current) => !current),
    togglePlayPause,
  }
}

export { getPlaybackScope, getPositionAlignedTime, useRecordingPlayback }
export type { TPlaybackStatus }
