import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useAnalysisWorkspacePanels } from './useAnalysisWorkspacePanels'
import type { IFrequencySpectrumAxis, IFrequencySpectrumSignal, ITimeWaveformAxis, ITimeWaveformSignal } from '../../types'

const waveformYAxis: ITimeWaveformAxis = {
  unit: 'FS',
  minimum: -1,
  maximum: 1,
  ticks: [-1, 0, 1],
}

const spectrumXAxis: IFrequencySpectrumAxis = {
  unit: 'Hz',
  minimum: 0,
  maximum: 22_050,
  ticks: [0, 5_512.5, 11_025, 16_537.5, 22_050],
}

const spectrumYAxis: IFrequencySpectrumAxis = {
  unit: 'dB rel.',
  minimum: -90,
  maximum: 0,
  ticks: [-90, -60, -30, 0],
}

const waveformSignals: ITimeWaveformSignal[] = [
  {
    signalId: 'signal-1',
    recordingId: 'recording-1',
    recordingFileName: 'recording.wav',
    displayName: 'Channel 1',
    durationSeconds: 1,
    sampleRate: 44_100,
    channelIndex: 0,
    amplitudeUnit: 'FS',
    isCalibrated: false,
    metrics: undefined,
    findings: [],
    bins: [],
  },
]

const spectrumSignals: IFrequencySpectrumSignal[] = [
  {
    signalId: 'signal-1',
    recordingId: 'recording-1',
    recordingFileName: 'recording.wav',
    displayName: 'Channel 1',
    durationSeconds: 1,
    sampleRate: 44_100,
    channelIndex: 0,
    amplitudeUnit: 'dB rel.',
    isCalibrated: false,
    metrics: undefined,
    findings: [],
    points: [],
  },
]

describe('useAnalysisWorkspacePanels', () => {
  it('builds a single waveform panel in focused mode', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspacePanels({
        chartWidth: 720,
        isSpectrumInitialLoading: false,
        isSpectrumRefreshing: false,
        isWaveformInitialLoading: false,
        isWaveformRefreshing: false,
        showSpectrumPanel: false,
        showWaveformPanel: true,
        spectrumError: null,
        spectrumSignals: [],
        spectrumXAxis: null,
        spectrumYAxis: null,
        waveformError: null,
        waveformSignals,
        waveformYAxis,
      })
    )

    expect(result.current.hasActiveChart).toBe(true)
    expect(result.current.panels).toHaveLength(1)
    expect(result.current.panels[0]?.surface).toBe('waveform')
    expect(result.current.panels[0]?.title).toBe('Waveform')
  })

  it('builds both panels in compare mode', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspacePanels({
        chartWidth: 720,
        isSpectrumInitialLoading: false,
        isSpectrumRefreshing: true,
        isWaveformInitialLoading: false,
        isWaveformRefreshing: false,
        showSpectrumPanel: true,
        showWaveformPanel: true,
        spectrumError: null,
        spectrumSignals,
        spectrumXAxis,
        spectrumYAxis,
        waveformError: null,
        waveformSignals,
        waveformYAxis,
      })
    )

    expect(result.current.hasActiveChart).toBe(true)
    expect(result.current.panels.map((panel) => panel.surface)).toEqual(['waveform', 'spectrum'])
    expect(result.current.panels[1]?.isRefreshing).toBe(true)
  })
})
