import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { RefObject } from 'react'
import type { ITimeWaveformRecording } from '../../types'
import type {
  TChannelAuditionRoute,
  TChannelRoutingStatus,
  TPlaybackSelectionOrigin,
} from '../types/channelAudition'

const maximumSupportedChannels = 32
const originalRoute: TChannelAuditionRoute = { mode: 'original' }

interface IChannelAuditionOption {
  channelIndex: number
  label: string
}

const getChannelAuditionOptions = (
  recording: ITimeWaveformRecording | null
): IChannelAuditionOption[] => {
  if (!recording || recording.channels <= 1) {
    return []
  }

  return Array.from({ length: recording.channels }, (_, channelIndex) => ({
    channelIndex,
    label: recording.signals.find((signal) => signal.channelIndex === channelIndex)?.displayName
      ?? `Channel ${channelIndex + 1}`,
  }))
}

const useChannelAuditionRouting = (
  audioRef: RefObject<HTMLAudioElement>,
  selectedRecording: ITimeWaveformRecording | null
) => {
  const contextRef = useRef<AudioContext | null>(null)
  const sourceRef = useRef<MediaElementAudioSourceNode | null>(null)
  const splitterRef = useRef<ChannelSplitterNode | null>(null)
  const mergerRef = useRef<ChannelMergerNode | null>(null)
  const routeRef = useRef<TChannelAuditionRoute>(originalRoute)
  const [route, setRoute] = useState<TChannelAuditionRoute>(originalRoute)
  const [status, setStatus] = useState<TChannelRoutingStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const options = useMemo(
    () => getChannelAuditionOptions(selectedRecording),
    [selectedRecording]
  )
  const isSupported = selectedRecording === null
    || selectedRecording.channels <= maximumSupportedChannels

  const disconnectProcessingNodes = useCallback(() => {
    splitterRef.current?.disconnect()
    mergerRef.current?.disconnect()
    splitterRef.current = null
    mergerRef.current = null
  }, [])

  const connectOriginal = useCallback(() => {
    const context = contextRef.current
    const source = sourceRef.current

    if (!context || !source) {
      return
    }

    source.disconnect()
    disconnectProcessingNodes()
    source.connect(context.destination)
  }, [disconnectProcessingNodes])

  const applyRoute = useCallback((
    nextRoute: TChannelAuditionRoute,
    recording: ITimeWaveformRecording
  ) => {
    const context = contextRef.current
    const source = sourceRef.current

    if (!context || !source) {
      return
    }

    source.disconnect()
    disconnectProcessingNodes()

    if (nextRoute.mode === 'original') {
      source.connect(context.destination)
      return
    }

    const splitter = context.createChannelSplitter(recording.channels)
    splitterRef.current = splitter
    const merger = context.createChannelMerger(2)
    mergerRef.current = merger
    source.connect(splitter)
    splitter.connect(merger, nextRoute.channelIndex, 0)
    splitter.connect(merger, nextRoute.channelIndex, 1)
    merger.connect(context.destination)
  }, [disconnectProcessingNodes])

  const restoreOriginalAfterFailure = useCallback(() => {
    try {
      connectOriginal()
    } catch {
      // The native element remains the safe fallback when graph recovery is unavailable.
    }

    routeRef.current = originalRoute
    setRoute(originalRoute)
    setStatus('error')
    setError('Channel routing unavailable.')
  }, [connectOriginal])

  const ensureGraph = useCallback(async () => {
    if (contextRef.current && sourceRef.current) {
      if (contextRef.current.state === 'suspended') {
        await contextRef.current.resume()
      }
      return { context: contextRef.current, source: sourceRef.current }
    }

    const audio = audioRef.current
    if (!audio || typeof window.AudioContext !== 'function') {
      throw new Error('Web Audio is unavailable.')
    }

    const context = new window.AudioContext()
    try {
      if (context.state === 'suspended') {
        await context.resume()
      }

      const source = context.createMediaElementSource(audio)
      contextRef.current = context
      sourceRef.current = source
      return { context, source }
    } catch (error) {
      void context.close()
      throw error
    }
  }, [audioRef])

  const selectRoute = useCallback(async (nextRoute: TChannelAuditionRoute) => {
    if (!selectedRecording) {
      return
    }

    if (
      nextRoute.mode === 'isolated'
      && (
        selectedRecording.channels > maximumSupportedChannels
        || nextRoute.channelIndex < 0
        || nextRoute.channelIndex >= selectedRecording.channels
      )
    ) {
      routeRef.current = originalRoute
      setRoute(originalRoute)
      setStatus('unsupported')
      setError('Channel routing unavailable for this recording.')
      return
    }

    if (nextRoute.mode === 'original' && !contextRef.current) {
      routeRef.current = originalRoute
      setRoute(originalRoute)
      setStatus('idle')
      setError(null)
      return
    }

    try {
      await ensureGraph()
      applyRoute(nextRoute, selectedRecording)
      routeRef.current = nextRoute
      setRoute(nextRoute)
      setStatus('ready')
      setError(null)
    } catch {
      restoreOriginalAfterFailure()
    }
  }, [applyRoute, ensureGraph, restoreOriginalAfterFailure, selectedRecording])

  const prepareForRecordingSelection = useCallback((
    recording: ITimeWaveformRecording,
    origin: TPlaybackSelectionOrigin
  ) => {
    const currentRoute = routeRef.current
    const canPreserve = origin === 'audition'
      && currentRoute.mode === 'isolated'
      && recording.channels <= maximumSupportedChannels
      && currentRoute.channelIndex < recording.channels
    const nextRoute = canPreserve ? currentRoute : originalRoute

    try {
      applyRoute(nextRoute, recording)
      routeRef.current = nextRoute
      setRoute(nextRoute)
      setStatus(contextRef.current ? 'ready' : 'idle')
      setError(null)
    } catch {
      restoreOriginalAfterFailure()
    }
  }, [applyRoute, restoreOriginalAfterFailure])

  const clearRoute = useCallback(() => {
    try {
      connectOriginal()
    } catch {
      // Clearing the media source still removes audible playback.
    }

    routeRef.current = originalRoute
    setRoute(originalRoute)
    setStatus('idle')
    setError(null)
  }, [connectOriginal])

  useEffect(() => () => {
    sourceRef.current?.disconnect()
    disconnectProcessingNodes()
    const context = contextRef.current
    contextRef.current = null
    sourceRef.current = null
    if (context) {
      void context.close()
    }
  }, [disconnectProcessingNodes])

  return {
    channelAuditionError: isSupported
      ? error
      : 'Channel routing unavailable for this recording.',
    channelAuditionOptions: options,
    channelAuditionRoute: route,
    channelRoutingStatus: isSupported ? status : 'unsupported',
    clearChannelAuditionRoute: clearRoute,
    isChannelAuditionSupported: isSupported,
    prepareChannelRouteForRecording: prepareForRecordingSelection,
    selectChannelAuditionRoute: selectRoute,
  }
}

export { getChannelAuditionOptions, maximumSupportedChannels, useChannelAuditionRouting }
export type { IChannelAuditionOption }
