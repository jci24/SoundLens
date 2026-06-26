import { useEffect, useMemo, useRef, useState, type RefObject } from 'react'
import { getFrequencySpectra } from '../services/frequencySpectra'
import { getTimeWaveforms } from '../services/timeWaveforms'
import {
  getSpectrumViewport,
  getVisibleSpectrumSignals,
  getVisibleSpectrumXAxis,
  type ISpectrumViewport,
} from '../utils/spectrumChart'
import type { IImportedFileSummary } from '../../../common/contracts/import'
import type {
  IFrequencySpectrumAxis,
  IFrequencySpectrumResponse,
  IFrequencySpectrumSignal,
  ITimeWaveformSignal,
  ITimeWaveformResponse,
} from '../types'
import { useMeasuredChartWidth } from './useMeasuredChartWidth'
import {
  areSignalIdsEqual,
  clampSpectrumRange,
  defaultSpectrumFftSize,
  defaultSpectrumRangeEndHz,
  formatSpectrumFftSizeOption,
  getNextExpandedRecordings,
  getNextRequestedSignalIds,
  getNextSpectrumRangeEnd,
  getNextSpectrumRangeStart,
  getOneSidedLineCount,
  getWaveformRequestedBinCount,
  spectrumFftSizeSelectOptions,
  type ISpectrumRange,
} from '../utils/analysisWorkspaceState'

export type TAnalysisSurface = 'waveform' | 'spectrum'
export type TAnalysisLayoutMode = 'focused' | 'compare'
export type TSignalChartMode = 'overlay' | 'split'

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
  onRecordingToggle: (recordingId: string) => void
  onSignalSelection: (signalId: string) => void
  onSignalChartModeChange: (mode: TSignalChartMode) => void
  onSpectrumPresetChange: (preset: string) => void
  onSpectrumRangeEndChange: (value: string) => void
  onSpectrumRangeReset: () => void
  onSpectrumRangeStartChange: (value: string) => void
  onSurfaceChange: (surface: TAnalysisSurface) => void
}

const useTimeWaveformWorkspace = (
  importedFiles: IImportedFileSummary[]
): IUseTimeWaveformWorkspaceResult => {
  const chartRef = useRef<HTMLDivElement | null>(null)
  const [activeSurface, setActiveSurface] = useState<TAnalysisSurface>('waveform')
  const [layoutMode, setLayoutMode] = useState<TAnalysisLayoutMode>('focused')
  const [signalChartMode, setSignalChartMode] = useState<TSignalChartMode>('overlay')
  const chartWidth = useMeasuredChartWidth(chartRef)
  const [waveforms, setWaveforms] = useState<ITimeWaveformResponse | null>(null)
  const [spectrum, setSpectrum] = useState<IFrequencySpectrumResponse | null>(null)
  const [waveformError, setWaveformError] = useState<string | null>(null)
  const [spectrumError, setSpectrumError] = useState<string | null>(null)
  const [requestedSignalIds, setRequestedSignalIds] = useState<string[]>([])
  const [expandedRecordings, setExpandedRecordings] = useState<string[]>([])
  const [selectedSpectrumFftSize, setSelectedSpectrumFftSize] = useState(defaultSpectrumFftSize)
  const [spectrumRange, setSpectrumRange] = useState<ISpectrumRange>({
    startHz: 0,
    endHz: defaultSpectrumRangeEndHz,
  })

  const binCount = useMemo(() => getWaveformRequestedBinCount(chartWidth), [chartWidth])
  const showWaveformPanel = layoutMode === 'compare' || activeSurface === 'waveform'
  const showSpectrumPanel = layoutMode === 'compare' || activeSurface === 'spectrum'

  useEffect(() => {
    if (layoutMode === 'compare' && signalChartMode !== 'overlay') {
      setSignalChartMode('overlay')
    }
  }, [layoutMode, signalChartMode])

  useEffect(() => {
    if (binCount <= 0 || importedFiles.length === 0) {
      return
    }

    let isCurrent = true

    const syncSignalIds = (responseSignalIds: string[]) => {
      setRequestedSignalIds((currentSignalIds) => {
        const nextSignalIds =
          currentSignalIds.length === 0
            ? responseSignalIds
            : currentSignalIds.filter((signalId) => responseSignalIds.includes(signalId))

        return areSignalIdsEqual(currentSignalIds, nextSignalIds)
          ? currentSignalIds
          : nextSignalIds
      })
    }

    if (showWaveformPanel) {
      void getTimeWaveforms(binCount, requestedSignalIds)
        .then((response) => {
          if (!isCurrent) {
            return
          }

          setWaveforms(response)
          setWaveformError(null)
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
      void getFrequencySpectra(getOneSidedLineCount(selectedSpectrumFftSize), requestedSignalIds)
        .then((response) => {
          if (!isCurrent) {
            return
          }

          setSpectrum(response)
          setSpectrumError(null)
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
  }, [binCount, importedFiles.length, requestedSignalIds, selectedSpectrumFftSize, showSpectrumPanel, showWaveformPanel])

  const waveformResponseSignalIds = waveforms?.selectedSignals.map((signal) => signal.signalId) ?? []
  const spectrumResponseSignalIds = spectrum?.selectedSignals.map((signal) => signal.signalId) ?? []
  const activeResponseSignalIds = activeSurface === 'waveform' ? waveformResponseSignalIds : spectrumResponseSignalIds
  const isWaveformRefreshing =
    showWaveformPanel &&
    binCount > 0 &&
    !waveformError &&
    (!waveforms ||
      waveforms.requestedBinCount !== binCount ||
      (requestedSignalIds.length > 0 &&
        !areSignalIdsEqual(requestedSignalIds, waveformResponseSignalIds)))
  const isWaveformInitialLoading = isWaveformRefreshing && waveforms === null
  const spectrumBinCount = getOneSidedLineCount(selectedSpectrumFftSize)
  const isSpectrumRefreshing =
    showSpectrumPanel &&
    binCount > 0 &&
    !spectrumError &&
    (!spectrum ||
      spectrum.requestedBinCount !== spectrumBinCount ||
      (requestedSignalIds.length > 0 &&
        !areSignalIdsEqual(requestedSignalIds, spectrumResponseSignalIds)))
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
  const selectedSignalIds = requestedSignalIds.length > 0 ? requestedSignalIds : activeResponseSignalIds
  const onRecordingToggle = (recordingId: string) => {
    setExpandedRecordings((currentExpanded) => getNextExpandedRecordings(currentExpanded, recordingId))
  }

  const onLayoutModeChange = (mode: TAnalysisLayoutMode) => {
    setLayoutMode(mode)
  }

  const onSignalSelection = (signalId: string) => {
    setRequestedSignalIds((currentSignalIds) => getNextRequestedSignalIds(currentSignalIds, signalId))
  }

  const onSignalChartModeChange = (mode: TSignalChartMode) => {
    setSignalChartMode(mode)
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
    selectedSignalIds,
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
    onRecordingToggle,
    onSignalSelection,
    onSignalChartModeChange,
    onSpectrumPresetChange,
    onSpectrumRangeEndChange,
    onSpectrumRangeReset,
    onSpectrumRangeStartChange,
    onSurfaceChange: setActiveSurface,
  }
}

export { useTimeWaveformWorkspace }
