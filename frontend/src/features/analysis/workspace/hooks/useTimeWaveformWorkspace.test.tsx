import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import { useTimeWaveformWorkspace } from './useTimeWaveformWorkspace'

const mockGetTimeWaveforms = vi.fn()
const mockGetFrequencySpectra = vi.fn()

vi.mock('../../services/timeWaveforms', () => ({
  getTimeWaveforms: (...args: unknown[]) => mockGetTimeWaveforms(...args),
}))

vi.mock('../../services/frequencySpectra', () => ({
  getFrequencySpectra: (...args: unknown[]) => mockGetFrequencySpectra(...args),
}))

vi.mock('./useMeasuredChartWidth', () => ({
  useMeasuredChartWidth: () => 256,
}))

const waveformResponse = {
  requestedBinCount: 256,
  recordings: [],
  selectedSignals: [],
  yAxis: {
    unit: 'FS',
    minimum: -1,
    maximum: 1,
    ticks: [-1, 0, 1],
  },
  regionOfInterest: null,
  failedFiles: [],
} as const

const spectrumResponse = {
  requestedBinCount: 22_051,
  recordings: [],
  selectedSignals: [],
  xAxis: {
    unit: 'Hz',
    minimum: 0,
    maximum: 22_050,
    ticks: [0, 11_025, 22_050],
  },
  yAxis: {
    unit: 'dB rel.',
    minimum: -90,
    maximum: 0,
    ticks: [-90, -45, 0],
  },
  analysis: {
    method: 'FFT',
    window: 'Hann',
    overlapPercent: 50,
    fftLength: 44_100,
    frequencyResolutionHz: 1,
    averagingMode: 'Linear',
    spectrumType: 'Magnitude',
    amplitudeUnit: 'dB rel.',
    isCalibrated: false,
  },
  regionOfInterest: {
    startTimeSeconds: 0.1,
    endTimeSeconds: 0.4,
    durationSeconds: 0.3,
  },
  failedFiles: [],
} as const

const importedFiles = [
  {
    fileName: 'alpha.wav',
    sizeBytes: 1_024,
    filePath: '/tmp/alpha.wav',
    contentType: 'audio/wav',
  },
]

const resetWorkspaceStore = () => {
  useAnalysisWorkspaceStore.setState({
    selectedSignalIds: [],
    expandedRecordings: [],
    activeSurface: 'waveform',
    layoutMode: 'focused',
    signalChartMode: 'overlay',
    regionOfInterest: null,
  })
}

describe('useTimeWaveformWorkspace', () => {
  beforeEach(() => {
    resetWorkspaceStore()
    mockGetTimeWaveforms.mockResolvedValue(waveformResponse)
    mockGetFrequencySpectra.mockResolvedValue(spectrumResponse)
  })

  afterEach(() => {
    resetWorkspaceStore()
    mockGetTimeWaveforms.mockReset()
    mockGetFrequencySpectra.mockReset()
  })

  it('keeps waveform requests full-length while sending ROI to spectrum requests', async () => {
    useAnalysisWorkspaceStore.setState({
      layoutMode: 'compare',
      regionOfInterest: {
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
        durationSeconds: 0.3,
      },
    })

    const { unmount } = renderHook(() => useTimeWaveformWorkspace(importedFiles.length))

    await waitFor(() => {
      expect(mockGetTimeWaveforms).toHaveBeenCalledWith(256, [])
      expect(mockGetFrequencySpectra).toHaveBeenCalledWith(44_100, [], {
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
      })
    })

    unmount()
  })

  it('synchronizes the default visible signal into the shared Copilot scope', async () => {
    mockGetTimeWaveforms.mockResolvedValue({
      ...waveformResponse,
      selectedSignals: [{ signalId: 'signal-default' }],
    })

    const { unmount } = renderHook(() => useTimeWaveformWorkspace(importedFiles.length))

    await waitFor(() => {
      expect(useAnalysisWorkspaceStore.getState().selectedSignalIds).toEqual(['signal-default'])
    })
    expect(mockGetTimeWaveforms).toHaveBeenLastCalledWith(256, ['signal-default'])

    unmount()
  })

  it('does not repeatedly request ROI spectrum data in compare mode after the first selection settles', async () => {
    useAnalysisWorkspaceStore.setState({
      layoutMode: 'compare',
      regionOfInterest: {
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
        durationSeconds: 0.3,
      },
    })

    const { result, unmount } = renderHook(() => useTimeWaveformWorkspace(importedFiles.length))

    await waitFor(() => {
      expect(result.current.isSpectrumRefreshing).toBe(false)
    })

    expect(mockGetFrequencySpectra).toHaveBeenCalledTimes(1)

    unmount()
  })

  it('treats a clamped ROI spectrum response as settled instead of perpetually refreshing', async () => {
    mockGetFrequencySpectra.mockResolvedValue({
      ...spectrumResponse,
      analysis: {
        ...spectrumResponse.analysis,
        fftLength: 1024,
        frequencyResolutionHz: 43.06640625,
      },
    })

    useAnalysisWorkspaceStore.setState({
      activeSurface: 'spectrum',
      layoutMode: 'focused',
      regionOfInterest: {
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
        durationSeconds: 0.3,
      },
    })

    const { result, unmount } = renderHook(() => useTimeWaveformWorkspace(importedFiles.length))

    await waitFor(() => {
      expect(result.current.isSpectrumRefreshing).toBe(false)
      expect(result.current.spectrum?.analysis.fftLength).toBe(1024)
    })

    unmount()
  })

  it('prefers the live workspace ROI over a stale echoed spectrum ROI after switching surfaces', async () => {
    useAnalysisWorkspaceStore.setState({
      activeSurface: 'waveform',
      layoutMode: 'focused',
      regionOfInterest: {
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
        durationSeconds: 0.3,
      },
    })

    const { result, rerender, unmount } = renderHook(() => useTimeWaveformWorkspace(importedFiles.length))

    await waitFor(() => {
      expect(result.current.regionOfInterest).toEqual({
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
        durationSeconds: 0.3,
      })
    })

    act(() => {
      useAnalysisWorkspaceStore.setState({
        activeSurface: 'spectrum',
      })
    })
    rerender()

    act(() => {
      useAnalysisWorkspaceStore.setState({
        activeSurface: 'waveform',
        regionOfInterest: {
          startTimeSeconds: 0.5,
          endTimeSeconds: 0.8,
          durationSeconds: 0.3,
        },
      })
    })
    rerender()

    expect(result.current.regionOfInterest).toEqual({
      startTimeSeconds: 0.5,
      endTimeSeconds: 0.8,
      durationSeconds: 0.3,
    })

    unmount()
  })

  it('lets the workspace clear and reselect ROI after switching between waveform and spectrum surfaces', async () => {
    useAnalysisWorkspaceStore.setState({
      activeSurface: 'waveform',
      layoutMode: 'focused',
      regionOfInterest: null,
    })

    const { result, rerender, unmount } = renderHook(() => useTimeWaveformWorkspace(importedFiles.length))

    await waitFor(() => {
      expect(result.current.waveforms).toEqual(waveformResponse)
    })

    act(() => {
      result.current.onRegionOfInterestChange({
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
        durationSeconds: 0.3,
      })
    })

    expect(result.current.regionOfInterest).toEqual({
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
      durationSeconds: 0.3,
    })

    act(() => {
      useAnalysisWorkspaceStore.setState({
        activeSurface: 'spectrum',
      })
    })
    rerender()

    expect(result.current.regionOfInterest).toEqual({
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
      durationSeconds: 0.3,
    })

    act(() => {
      result.current.onRegionOfInterestChange(null)
    })

    expect(result.current.regionOfInterest).toBeNull()

    act(() => {
      useAnalysisWorkspaceStore.setState({
        activeSurface: 'waveform',
      })
    })
    rerender()

    act(() => {
      result.current.onRegionOfInterestChange({
        startTimeSeconds: 0.5,
        endTimeSeconds: 0.8,
        durationSeconds: 0.3,
      })
    })

    expect(result.current.regionOfInterest).toEqual({
      startTimeSeconds: 0.5,
      endTimeSeconds: 0.8,
      durationSeconds: 0.3,
    })

    unmount()
  })
})
