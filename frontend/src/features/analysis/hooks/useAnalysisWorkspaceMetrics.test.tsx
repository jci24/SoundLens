import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useAnalysisWorkspaceMetrics } from './useAnalysisWorkspaceMetrics'

describe('useAnalysisWorkspaceMetrics', () => {
  it('prefers waveform metrics when waveform signals are available', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceMetrics({
        spectrumSignals: [],
        waveformSignals: [
          {
            signalId: 'wave-1',
            recordingId: 'recording-1',
            recordingFileName: 'wave.wav',
            displayName: 'Channel 1',
            durationSeconds: 2,
            sampleRate: 44_100,
            channelIndex: 0,
            amplitudeUnit: 'FS',
            isCalibrated: false,
            metrics: {
              peakAmplitude: 1,
              rmsAmplitude: 0.7,
              crestFactor: 1.43,
              clippingSampleCount: 2,
              hasClipping: true,
            },
            bins: [],
          },
        ],
      })
    )

    expect(result.current.metricSignals).toEqual([
      {
        signalId: 'wave-1',
        recordingFileName: 'wave.wav',
        displayName: 'Channel 1',
        durationSeconds: 2,
        sampleRate: 44_100,
        peakAmplitude: 1,
        rmsAmplitude: 0.7,
        crestFactor: 1.43,
        clippingSampleCount: 2,
        hasClipping: true,
      },
    ])
    expect(result.current.hasMetricsPending).toBe(false)
  })

  it('maps spectrum metrics when the spectrum surface is active', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceMetrics({
        spectrumSignals: [
          {
            signalId: 'spec-1',
            recordingId: 'recording-1',
            recordingFileName: 'spec.wav',
            displayName: 'Channel 2',
            durationSeconds: 1.5,
            sampleRate: 48_000,
            channelIndex: 1,
            amplitudeUnit: 'dB rel.',
            isCalibrated: false,
            metrics: {
              peakAmplitude: 0.82,
              rmsAmplitude: 0.58,
              crestFactor: 1.41,
              clippingSampleCount: 0,
              hasClipping: false,
            },
            points: [],
          },
        ],
        waveformSignals: [],
      })
    )

    expect(result.current.metricSignals[0].displayName).toBe('Channel 2')
    expect(result.current.metricSignals[0].sampleRate).toBe(48_000)
    expect(result.current.metricSignals[0].hasClipping).toBe(false)
    expect(result.current.hasMetricsPending).toBe(false)
  })

  it('falls back safely when a signal does not yet include derived metrics', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceMetrics({
        spectrumSignals: [],
        waveformSignals: [
          {
            signalId: 'wave-legacy',
            recordingId: 'recording-1',
            recordingFileName: 'legacy.wav',
            displayName: 'Channel 1',
            durationSeconds: 1,
            sampleRate: 44_100,
            channelIndex: 0,
            amplitudeUnit: 'FS',
            isCalibrated: false,
            bins: [],
          },
        ],
      })
    )

    expect(result.current.metricSignals[0]).toMatchObject({
      signalId: 'wave-legacy',
      peakAmplitude: 0,
      rmsAmplitude: 0,
      crestFactor: 0,
      clippingSampleCount: 0,
      hasClipping: false,
    })
    expect(result.current.hasMetricsPending).toBe(true)
  })
})
