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

interface IUseTimeWaveformWorkspaceResult {
  activeSurface: TAnalysisSurface
  chartRef: RefObject<HTMLDivElement | null>
  chartWidth: number
  error: string | null
  expandedRecordings: string[]
  isInitialLoading: boolean
  isRefreshing: boolean
  spectrumFftSizeOptions: string[]
  spectrumMaximumHz: number
  spectrumRangeEndHz: number
  spectrumRangeStartHz: number
  spectrumViewport: ISpectrumViewport | null
  selectedSpectrumPreset: string
  spectrum: IFrequencySpectrumResponse | null
  spectrumXAxis: IFrequencySpectrumAxis | null
  spectrumSignals: IFrequencySpectrumSignal[]
  waveformSignals: ITimeWaveformSignal[]
  recordings: ITimeWaveformResponse['recordings']
  selectedSignalIds: string[]
  waveforms: ITimeWaveformResponse | null
  onRecordingToggle: (recordingId: string) => void
  onSignalSelection: (signalId: string) => void
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
  const chartWidth = useMeasuredChartWidth(chartRef)
  const [waveforms, setWaveforms] = useState<ITimeWaveformResponse | null>(null)
  const [spectrum, setSpectrum] = useState<IFrequencySpectrumResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [requestedSignalIds, setRequestedSignalIds] = useState<string[]>([])
  const [expandedRecordings, setExpandedRecordings] = useState<string[]>([])
  const [selectedSpectrumFftSize, setSelectedSpectrumFftSize] = useState(defaultSpectrumFftSize)
  const [spectrumRange, setSpectrumRange] = useState<ISpectrumRange>({
    startHz: 0,
    endHz: defaultSpectrumRangeEndHz,
  })

  const binCount = useMemo(() => getWaveformRequestedBinCount(chartWidth), [chartWidth])

  useEffect(() => {
    if (binCount <= 0 || importedFiles.length === 0) return

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

    if (activeSurface === 'waveform') {
      void getTimeWaveforms(binCount, requestedSignalIds)
        .then((response) => {
          if (!isCurrent) return

          setWaveforms(response)
          setError(null)
          syncSignalIds(response.selectedSignals.map((signal) => signal.signalId))
        })
        .catch((caughtError) => {
          if (!isCurrent) return

          setError(caughtError instanceof Error ? caughtError.message : 'Waveform data could not be generated.')
        })
    } else {
      void getFrequencySpectra(getOneSidedLineCount(selectedSpectrumFftSize), requestedSignalIds)
        .then((response) => {
          if (!isCurrent) return

          setSpectrum(response)
          setError(null)
          syncSignalIds(response.selectedSignals.map((signal) => signal.signalId))
        })
        .catch((caughtError) => {
          if (!isCurrent) return

          setError(caughtError instanceof Error ? caughtError.message : 'Spectrum data could not be generated.')
        })
    }

    return () => {
      isCurrent = false
    }
  }, [activeSurface, binCount, importedFiles.length, requestedSignalIds, selectedSpectrumFftSize])

  const activeResponse =
    activeSurface === 'waveform'
      ? waveforms
      : spectrum
  const activeResponseSignalIds = activeResponse?.selectedSignals.map((signal) => signal.signalId) ?? []
  const activeRequestedBinCount = activeSurface === 'waveform' ? binCount : getOneSidedLineCount(selectedSpectrumFftSize)
  const isRefreshing =
    binCount > 0 &&
    !error &&
    (!activeResponse ||
      activeResponse.requestedBinCount !== activeRequestedBinCount ||
      (requestedSignalIds.length > 0 &&
        !areSignalIdsEqual(requestedSignalIds, activeResponseSignalIds)))
  const isInitialLoading = isRefreshing && activeResponse === null

  const recordings =
    activeResponse?.recordings ??
    waveforms?.recordings ??
    spectrum?.recordings ??
    []
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
    () =>
      spectrumViewport
        ? getVisibleSpectrumSignals(fullSpectrumSignals, spectrumViewport)
        : fullSpectrumSignals,
    [fullSpectrumSignals, spectrumViewport]
  )
  const spectrumXAxis = useMemo(
    () =>
      fullSpectrumXAxis && spectrumViewport
        ? getVisibleSpectrumXAxis(fullSpectrumXAxis, spectrumViewport)
        : fullSpectrumXAxis,
    [fullSpectrumXAxis, spectrumViewport]
  )
  const selectedSignalIds =
    requestedSignalIds.length > 0
      ? requestedSignalIds
      : activeResponseSignalIds

  const onRecordingToggle = (recordingId: string) => {
    setExpandedRecordings((currentExpanded) => getNextExpandedRecordings(currentExpanded, recordingId))
  }

  const onSignalSelection = (signalId: string) => {
    setRequestedSignalIds((currentSignalIds) => getNextRequestedSignalIds(currentSignalIds, signalId))
  }

  const onSpectrumPresetChange = (preset: string) => {
    const selectedOption = spectrumFftSizeSelectOptions.find((option) => option.label === preset)
    if (!selectedOption) return

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
    error,
    expandedRecordings,
    isInitialLoading,
    isRefreshing,
    spectrumFftSizeOptions: spectrumFftSizeSelectOptions.map((option) => option.label),
    spectrumMaximumHz,
    spectrumRangeEndHz: clampedSpectrumRangeEndHz,
    spectrumRangeStartHz: clampedSpectrumRangeStartHz,
    spectrumViewport,
    selectedSpectrumPreset: formatSpectrumFftSizeOption(selectedSpectrumFftSize),
    spectrum,
    spectrumXAxis,
    spectrumSignals,
    waveformSignals,
    recordings,
    selectedSignalIds,
    waveforms,
    onRecordingToggle,
    onSignalSelection,
    onSpectrumPresetChange,
    onSpectrumRangeEndChange,
    onSpectrumRangeReset,
    onSpectrumRangeStartChange,
    onSurfaceChange: setActiveSurface,
  }
}

export { useTimeWaveformWorkspace }
