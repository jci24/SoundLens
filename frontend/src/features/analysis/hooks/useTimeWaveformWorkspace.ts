import { useEffect, useMemo, useRef, useState, type MouseEvent, type RefObject } from 'react'
import { getTimeWaveforms } from '../services/timeWaveforms'
import type { IImportedFileSummary, ITimeWaveformResponse } from '../../import/types'

interface IUseTimeWaveformWorkspaceResult {
  chartRef: RefObject<HTMLDivElement | null>
  chartWidth: number
  error: string | null
  expandedRecordings: string[]
  isInitialLoading: boolean
  isRefreshing: boolean
  isWaveformVisible: boolean
  recordings: ITimeWaveformResponse['recordings']
  selectedSignals: ITimeWaveformResponse['selectedSignals']
  yAxis: ITimeWaveformResponse['yAxis'] | null
  onSignalSelection: (signalId: string, event: MouseEvent<HTMLButtonElement>) => void
  onRecordingToggle: (recordingId: string) => void
}

const useTimeWaveformWorkspace = (
  importedFiles: IImportedFileSummary[]
): IUseTimeWaveformWorkspaceResult => {
  const chartRef = useRef<HTMLDivElement | null>(null)
  const [chartWidth, setChartWidth] = useState(0)
  const [waveforms, setWaveforms] = useState<ITimeWaveformResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [requestedSignalIds, setRequestedSignalIds] = useState<string[]>([])
  const [expandedRecordings, setExpandedRecordings] = useState<string[]>([])

  useEffect(() => {
    if (!chartRef.current) return

    const observer = new ResizeObserver(([entry]) => {
      setChartWidth(Math.floor(entry.contentRect.width))
    })

    observer.observe(chartRef.current)
    return () => observer.disconnect()
  }, [])

  const binCount = useMemo(() => {
    if (chartWidth <= 0) return 0

    const deviceScale = Math.max(2, window.devicePixelRatio || 1)
    return Math.min(4000, Math.max(64, Math.ceil(chartWidth * deviceScale)))
  }, [chartWidth])

  useEffect(() => {
    if (binCount <= 0 || importedFiles.length === 0) return

    let isCurrent = true

    void getTimeWaveforms(binCount, requestedSignalIds)
      .then((response) => {
        if (!isCurrent) return

        setWaveforms(response)
        setError(null)
        setRequestedSignalIds((currentSignalIds) => {
          const responseSignalIds = response.selectedSignals.map((signal) => signal.signalId)
          const nextSignalIds =
            currentSignalIds.length === 0
              ? responseSignalIds
              : currentSignalIds.filter((signalId) =>
                  response.selectedSignals.some((signal) => signal.signalId === signalId)
                )

          return areSignalIdsEqual(currentSignalIds, nextSignalIds)
            ? currentSignalIds
            : nextSignalIds
        })
      })
      .catch((caughtError) => {
        if (!isCurrent) return

        setError(caughtError instanceof Error ? caughtError.message : 'Waveform data could not be generated.')
      })

    return () => {
      isCurrent = false
    }
  }, [binCount, importedFiles.length, requestedSignalIds])

  const isRefreshing =
    binCount > 0 &&
    !error &&
    (!waveforms ||
      waveforms.requestedBinCount !== binCount ||
      (requestedSignalIds.length > 0 &&
        !areSignalIdsEqual(
          requestedSignalIds,
          waveforms.selectedSignals.map((signal) => signal.signalId)
        )))
  const isInitialLoading = isRefreshing && waveforms === null

  const recordings = waveforms?.recordings ?? []
  const selectedSignals = waveforms?.selectedSignals ?? []
  const yAxis = waveforms?.yAxis ?? null
  const isWaveformVisible = !error && selectedSignals.length > 0 && chartWidth > 0

  const onRecordingToggle = (recordingId: string) => {
    setExpandedRecordings((currentExpanded) =>
      currentExpanded.includes(recordingId)
        ? currentExpanded.filter((expandedPath) => expandedPath !== recordingId)
        : [...currentExpanded, recordingId]
    )
  }

  const onSignalSelection = (
    signalId: string,
    event: MouseEvent<HTMLButtonElement>
  ) => {
    const isAdditiveSelection = event.metaKey || event.ctrlKey

    setRequestedSignalIds((currentSignalIds) => {
      if (!isAdditiveSelection) {
        return [signalId]
      }

      return currentSignalIds.includes(signalId)
        ? currentSignalIds.filter((currentSignalId) => currentSignalId !== signalId)
        : [...currentSignalIds, signalId]
    })
  }

  return {
    chartRef,
    chartWidth,
    error,
    expandedRecordings,
    isInitialLoading,
    isRefreshing,
    isWaveformVisible,
    recordings,
    selectedSignals,
    yAxis,
    onSignalSelection,
    onRecordingToggle,
  }
}

// Selection order drives overlay order, so this intentionally compares sequences, not sets.
const areSignalIdsEqual = (left: string[], right: string[]) =>
  left.length === right.length && left.every((signalId, index) => signalId === right[index])

export { useTimeWaveformWorkspace }
