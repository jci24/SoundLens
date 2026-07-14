import { useDeferredValue, useEffect, useMemo, useRef, useState, type RefObject } from 'react'
import { getFrequencySpectra } from '../../services/frequencySpectra'
import { getTimeWaveforms } from '../../services/timeWaveforms'
import {
  getSpectrumViewport,
  getVisibleSpectrumSignals,
  getVisibleSpectrumXAxis,
  type ISpectrumViewport,
} from '../../spectrum/utils/spectrumChart'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import type {
  IAnalysisRegionOfInterest,
  IFrequencySpectrumAxis,
  IFrequencySpectrumResponse,
  IFrequencySpectrumSignal,
  ITimeWaveformSignal,
  ITimeWaveformResponse,
  TAnalysisLayoutMode,
  TAnalysisSurface,
  TComparisonGroupAssignment,
  TSignalChartMode,
} from '../../types'
import { useMeasuredChartWidth } from './useMeasuredChartWidth'
import {
  areRegionOfInterestEqual,
  areSignalIdsEqual,
  clampSpectrumRange,
  defaultSpectrumFftSize,
  defaultSpectrumRangeEndHz,
  formatSpectrumFftSizeOption,
  getNextSpectrumRangeEnd,
  getNextSpectrumRangeStart,
  getWaveformRequestedBinCount,
  spectrumFftSizeSelectOptions,
  type IRequestedRegionOfInterest,
  type ISpectrumRange,
} from '../../utils/analysisWorkspaceState'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'

interface IUseTimeWaveformWorkspaceResult {
  activeSurface: TAnalysisSurface
  chartRef: RefObject<HTMLDivElement | null>
  chartWidth: number
  expandedRecordings: string[]
  isSpectrumInitialLoading: boolean
  isSpectrumRefreshing: boolean
  isWaveformInitialLoading: boolean
  isWaveformRefreshing: boolean
  layoutMode: TAnalysisLayoutMode
  recordings: ITimeWaveformResponse['recordings']
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  selectedSignalIds: string[]
  selectedSpectrumPreset: string
  signalChartMode: TSignalChartMode
  showSpectrumPanel: boolean
  showWaveformPanel: boolean
  spectrum: IFrequencySpectrumResponse | null
  spectrumError: string | null
  spectrumFftSizeOptions: string[]
  spectrumMaximumHz: number
  spectrumRangeEndHz: number
  spectrumRangeStartHz: number
  spectrumSignals: IFrequencySpectrumSignal[]
  spectrumViewport: ISpectrumViewport | null
  spectrumXAxis: IFrequencySpectrumAxis | null
  waveformError: string | null
  waveformSignals: ITimeWaveformSignal[]
  waveforms: ITimeWaveformResponse | null
  onLayoutModeChange: (mode: TAnalysisLayoutMode) => void
  onRecordingGroupAssignment: (recordingId: string, assignment: TComparisonGroupAssignment) => void
  onComparisonTargetsSwap: () => void
  onRecordingToggle: (recordingId: string) => void
  onSignalSelection: (signalId: string) => void
  onSignalChartModeChange: (mode: TSignalChartMode) => void
  onSpectrumPresetChange: (preset: string) => void
  onSpectrumRangeEndChange: (value: string) => void
  onSpectrumRangeReset: () => void
  onSpectrumRangeStartChange: (value: string) => void
  onSurfaceChange: (surface: TAnalysisSurface) => void
  onRegionOfInterestChange: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  regionOfInterest: IAnalysisRegionOfInterest | null
}

interface IResolvedSpectrumRequest {
  fftSize: number
  regionOfInterest: IRequestedRegionOfInterest | null
  signalIds: string[]
}

const useTimeWaveformWorkspace = (
  importedFiles: IImportedFileSummary[]
): IUseTimeWaveformWorkspaceResult => {
  const chartRef = useRef<HTMLDivElement | null>(null)
  const chartWidth = useMeasuredChartWidth(chartRef)
  const deferredChartWidth = useDeferredValue(chartWidth)
  const [waveforms, setWaveforms] = useState<ITimeWaveformResponse | null>(null)
  const [spectrum, setSpectrum] = useState<IFrequencySpectrumResponse | null>(null)
  const [waveformError, setWaveformError] = useState<string | null>(null)
  const [spectrumError, setSpectrumError] = useState<string | null>(null)
  const [resolvedSpectrumRequest, setResolvedSpectrumRequest] = useState<IResolvedSpectrumRequest | null>(null)
  const [selectedSpectrumFftSize, setSelectedSpectrumFftSize] = useState(defaultSpectrumFftSize)
  const [spectrumRange, setSpectrumRange] = useState<ISpectrumRange>({
    startHz: 0,
    endHz: defaultSpectrumRangeEndHz,
  })

  const activeSurface = useAnalysisWorkspaceStore((state) => state.activeSurface)
  const layoutMode = useAnalysisWorkspaceStore((state) => state.layoutMode)
  const signalChartMode = useAnalysisWorkspaceStore((state) => state.signalChartMode)
  const selectedSignalIds = useAnalysisWorkspaceStore((state) => state.selectedSignalIds)
  const expandedRecordings = useAnalysisWorkspaceStore((state) => state.expandedRecordings)
  const recordingGroupAssignments = useAnalysisWorkspaceStore((state) => state.recordingGroupAssignments)
  const regionOfInterest = useAnalysisWorkspaceStore((state) => state.regionOfInterest)
  const syncSignalIds = useAnalysisWorkspaceStore((state) => state.syncSignalIds)
  const setRegionOfInterest = useAnalysisWorkspaceStore((state) => state.setRegionOfInterest)
  const setRecordings = useAnalysisWorkspaceStore((state) => state.setRecordings)

  const binCount = useMemo(() => getWaveformRequestedBinCount(deferredChartWidth), [deferredChartWidth])
  const showWaveformPanel = layoutMode === 'compare' || activeSurface === 'waveform'
  const showSpectrumPanel = layoutMode === 'compare' || activeSurface === 'spectrum'
  const requestedRegionOfInterest = useMemo<IRequestedRegionOfInterest | null>(
    () =>
      regionOfInterest
        ? {
            startTimeSeconds: regionOfInterest.startTimeSeconds,
            endTimeSeconds: regionOfInterest.endTimeSeconds,
          }
        : null,
    [regionOfInterest]
  )

  useEffect(() => {
    if (binCount <= 0 || importedFiles.length === 0) {
      return
    }

    let isCurrent = true

    if (showWaveformPanel) {
      void getTimeWaveforms(binCount, selectedSignalIds)
        .then((response) => {
          if (!isCurrent) {
            return
          }

          setWaveforms(response)
          setWaveformError(null)
          setRecordings(response.recordings)
          syncSignalIds(response.selectedSignals.map((signal) => signal.signalId))
        })
        .catch((caughtError) => {
          if (!isCurrent) {
            return
          }

          setWaveformError(caughtError instanceof Error ? caughtError.message : 'Waveform data could not be generated.')
        })
    }

    if (showSpectrumPanel) {
      void getFrequencySpectra(selectedSpectrumFftSize, selectedSignalIds, requestedRegionOfInterest)
        .then((response) => {
          if (!isCurrent) {
            return
          }

          setSpectrum(response)
          setSpectrumError(null)
          setResolvedSpectrumRequest({
            fftSize: selectedSpectrumFftSize,
            regionOfInterest: requestedRegionOfInterest,
            signalIds: [...selectedSignalIds],
          })
          syncSignalIds(response.selectedSignals.map((signal) => signal.signalId))
        })
        .catch((caughtError) => {
          if (!isCurrent) {
            return
          }

          setSpectrumError(caughtError instanceof Error ? caughtError.message : 'Spectrum data could not be generated.')
        })
    }

    return () => {
      isCurrent = false
    }
  }, [binCount, importedFiles.length, requestedRegionOfInterest, selectedSignalIds, selectedSpectrumFftSize, showSpectrumPanel, showWaveformPanel, syncSignalIds, setRecordings])

  const waveformResponseSignalIds = waveforms?.selectedSignals.map((signal) => signal.signalId) ?? []
  const spectrumResponseSignalIds = spectrum?.selectedSignals.map((signal) => signal.signalId) ?? []
  const spectrumResponseRegionOfInterest = spectrum?.regionOfInterest
    ? {
        startTimeSeconds: spectrum.regionOfInterest.startTimeSeconds,
        endTimeSeconds: spectrum.regionOfInterest.endTimeSeconds,
      }
    : null
  const activeResponseSignalIds = activeSurface === 'waveform' ? waveformResponseSignalIds : spectrumResponseSignalIds
  const isWaveformRefreshing =
    showWaveformPanel &&
    binCount > 0 &&
    !waveformError &&
    (!waveforms ||
      waveforms.requestedBinCount !== binCount ||
      (selectedSignalIds.length > 0 &&
        !areSignalIdsEqual(selectedSignalIds, waveformResponseSignalIds)))
  const isWaveformInitialLoading = isWaveformRefreshing && waveforms === null
  const isSpectrumRefreshing =
    showSpectrumPanel &&
    binCount > 0 &&
    !spectrumError &&
    (!spectrum ||
      resolvedSpectrumRequest?.fftSize !== selectedSpectrumFftSize ||
      !areRegionOfInterestEqual(requestedRegionOfInterest, resolvedSpectrumRequest.regionOfInterest ?? null) ||
      !areRegionOfInterestEqual(requestedRegionOfInterest, spectrumResponseRegionOfInterest) ||
      (selectedSignalIds.length > 0 &&
        !areSignalIdsEqual(selectedSignalIds, resolvedSpectrumRequest?.signalIds ?? [])))
  const isSpectrumInitialLoading = isSpectrumRefreshing && spectrum === null

  const recordings = waveforms?.recordings ?? spectrum?.recordings ?? []
  const waveformSignals = useMemo(() => waveforms?.selectedSignals ?? [], [waveforms])
  const fullSpectrumSignals = useMemo(() => spectrum?.selectedSignals ?? [], [spectrum])
  const fullSpectrumXAxis = spectrum?.xAxis ?? null
  const spectrumMaximumHz = fullSpectrumXAxis?.maximum ?? defaultSpectrumRangeEndHz
  const clampedSpectrumRange = useMemo(
    () => clampSpectrumRange(spectrumRange, spectrumMaximumHz),
    [spectrumMaximumHz, spectrumRange]
  )
  const clampedSpectrumRangeStartHz = clampedSpectrumRange.startHz
  const clampedSpectrumRangeEndHz = clampedSpectrumRange.endHz
  const spectrumViewport = useMemo(
    () =>
      fullSpectrumXAxis
        ? getSpectrumViewport(fullSpectrumXAxis, clampedSpectrumRangeStartHz, clampedSpectrumRangeEndHz)
        : null,
    [clampedSpectrumRangeEndHz, clampedSpectrumRangeStartHz, fullSpectrumXAxis]
  )
  const spectrumSignals = useMemo(
    () => (spectrumViewport ? getVisibleSpectrumSignals(fullSpectrumSignals, spectrumViewport) : fullSpectrumSignals),
    [fullSpectrumSignals, spectrumViewport]
  )
  const spectrumXAxis = useMemo(
    () => (fullSpectrumXAxis && spectrumViewport ? getVisibleSpectrumXAxis(fullSpectrumXAxis, spectrumViewport) : fullSpectrumXAxis),
    [fullSpectrumXAxis, spectrumViewport]
  )
  const resolvedSelectedSignalIds = selectedSignalIds.length > 0 ? selectedSignalIds : activeResponseSignalIds
  const resolvedRegionOfInterest = regionOfInterest ?? spectrum?.regionOfInterest ?? null

  const onRecordingToggle = useAnalysisWorkspaceStore((state) => state.toggleRecording)
  const onRecordingGroupAssignment = useAnalysisWorkspaceStore(
    (state) => state.setRecordingGroupAssignment
  )
  const onComparisonTargetsSwap = useAnalysisWorkspaceStore((state) => state.swapComparisonTargets)
  const onLayoutModeChange = useAnalysisWorkspaceStore((state) => state.setLayoutMode)
  const onSignalSelection = useAnalysisWorkspaceStore((state) => state.selectSignal)
  const onSignalChartModeChange = useAnalysisWorkspaceStore((state) => state.setSignalChartMode)
  const onSurfaceChange = useAnalysisWorkspaceStore((state) => state.setActiveSurface)
  const onRegionOfInterestChange = (nextRegionOfInterest: IAnalysisRegionOfInterest | null) => {
    setRegionOfInterest(nextRegionOfInterest)
  }

  const onSpectrumPresetChange = (preset: string) => {
    const selectedOption = spectrumFftSizeSelectOptions.find((option) => option.label === preset)
    if (!selectedOption) {
      return
    }

    setSelectedSpectrumFftSize(selectedOption.value)
  }

  const onSpectrumRangeStartChange = (value: string) => {
    const parsedValue = Number(value)
    if (Number.isNaN(parsedValue)) {
      return
    }

    setSpectrumRange((currentRange) => getNextSpectrumRangeStart(parsedValue, currentRange, spectrumMaximumHz))
  }

  const onSpectrumRangeEndChange = (value: string) => {
    const parsedValue = Number(value)
    if (Number.isNaN(parsedValue)) {
      return
    }

    setSpectrumRange((currentRange) => getNextSpectrumRangeEnd(parsedValue, currentRange, spectrumMaximumHz))
  }

  const onSpectrumRangeReset = () => {
    setSpectrumRange({
      startHz: 0,
      endHz: spectrumMaximumHz,
    })
  }

  return {
    activeSurface,
    chartRef,
    chartWidth,
    expandedRecordings,
    isSpectrumInitialLoading,
    isSpectrumRefreshing,
    isWaveformInitialLoading,
    isWaveformRefreshing,
    layoutMode,
    recordings,
    recordingGroupAssignments,
    selectedSignalIds: resolvedSelectedSignalIds,
    selectedSpectrumPreset: formatSpectrumFftSizeOption(selectedSpectrumFftSize),
    signalChartMode,
    showSpectrumPanel,
    showWaveformPanel,
    spectrum,
    spectrumError,
    spectrumFftSizeOptions: spectrumFftSizeSelectOptions.map((option) => option.label),
    spectrumMaximumHz,
    spectrumRangeEndHz: clampedSpectrumRangeEndHz,
    spectrumRangeStartHz: clampedSpectrumRangeStartHz,
    spectrumSignals,
    spectrumViewport,
    spectrumXAxis,
    waveformError,
    waveformSignals,
    waveforms,
    onLayoutModeChange,
    onComparisonTargetsSwap,
    onRecordingGroupAssignment,
    onRecordingToggle,
    onSignalSelection,
    onSignalChartModeChange,
    onSpectrumPresetChange,
    onSpectrumRangeEndChange,
    onSpectrumRangeReset,
    onSpectrumRangeStartChange,
    onSurfaceChange,
    onRegionOfInterestChange,
    regionOfInterest: resolvedRegionOfInterest,
  }
}

export { useTimeWaveformWorkspace }
