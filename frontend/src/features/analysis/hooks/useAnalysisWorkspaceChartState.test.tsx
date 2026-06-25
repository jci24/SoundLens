import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useAnalysisWorkspaceChartState } from './useAnalysisWorkspaceChartState'
import type { IFrequencySpectrumResponse, ITimeWaveformResponse } from '../types'

const waveformResponse: ITimeWaveformResponse = {
  requestedBinCount: 256,
  recordings: [],
  selectedSignals: [
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
      points: [],
    },
  ],
  yAxis: {
    unit: 'FS',
    minimum: -1,
    maximum: 1,
    ticks: [-1, 0, 1],
  },
  failedFiles: [],
}

const spectrumResponse: IFrequencySpectrumResponse = {
  requestedBinCount: 2049,
  recordings: [],
  selectedSignals: [
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
      points: [],
    },
  ],
  xAxis: {
    unit: 'Hz',
    minimum: 0,
    maximum: 22_050,
    ticks: [0, 5_512.5, 11_025, 16_537.5, 22_050],
  },
  yAxis: {
    unit: 'dB rel.',
    minimum: -90,
    maximum: 0,
    ticks: [-90, -60, -30, 0],
  },
  analysis: {
    method: 'FFT',
    window: 'Hann',
    overlapPercent: 50,
    fftLength: 4_096,
    frequencyResolutionHz: 10.8,
    averagingMode: 'Linear',
    spectrumType: 'Magnitude',
    amplitudeUnit: 'dB rel.',
    isCalibrated: false,
  },
  failedFiles: [],
}

describe('useAnalysisWorkspaceChartState', () => {
  it('reports a waveform chart as active only when there is width, data, and an axis', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceChartState({
        activeSurface: 'waveform',
        chartWidth: 720,
        spectrum: null,
        spectrumSignals: [],
        spectrumXAxis: null,
        waveforms: waveformResponse,
        waveformSignals: waveformResponse.selectedSignals,
      })
    )

    expect(result.current.hasActiveChart).toBe(true)
    expect(result.current.loadingLabel).toBe('Generating waveform bins')
    expect(result.current.refreshingLabel).toBe('Updating overlay')
    expect(result.current.waveformYAxis).toEqual(waveformResponse.yAxis)
  })

  it('reports a spectrum chart as inactive without an x axis', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceChartState({
        activeSurface: 'spectrum',
        chartWidth: 720,
        spectrum: spectrumResponse,
        spectrumSignals: spectrumResponse.selectedSignals,
        spectrumXAxis: null,
        waveforms: null,
        waveformSignals: [],
      })
    )

    expect(result.current.hasActiveChart).toBe(false)
    expect(result.current.loadingLabel).toBe('Generating spectrum bins')
    expect(result.current.refreshingLabel).toBe('Updating spectrum')
    expect(result.current.spectrumYAxis).toEqual(spectrumResponse.yAxis)
  })
})
