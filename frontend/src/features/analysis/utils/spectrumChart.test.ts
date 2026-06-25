import { describe, expect, it } from 'vitest'
import {
  getNearestSpectrumPoint,
  getSpectrumViewport,
  getVisibleSpectrumSignals,
  getVisibleSpectrumXAxis,
} from './spectrumChart'
import type { IFrequencySpectrumAxis, IFrequencySpectrumSignal } from '../types'

const xAxis: IFrequencySpectrumAxis = {
  unit: 'Hz',
  minimum: 0,
  maximum: 22_050,
  ticks: [0, 5_512.5, 11_025, 16_537.5, 22_050],
}

const signals: IFrequencySpectrumSignal[] = [
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
    points: [
      { frequencyHz: 0, value: -80 },
      { frequencyHz: 1_000, value: -20 },
      { frequencyHz: 2_000, value: -35 },
      { frequencyHz: 3_000, value: -45 },
    ],
  },
]

describe('spectrumChart', () => {
  it('clamps the spectrum viewport to the available axis bounds', () => {
    expect(getSpectrumViewport(xAxis, -10, 40_000)).toEqual({
      startHz: 0,
      endHz: 22_050,
    })
  })

  it('filters visible spectrum points using an exclusive end bound', () => {
    const visibleSignals = getVisibleSpectrumSignals(signals, {
      startHz: 1_000,
      endHz: 10_000,
    })

    expect(visibleSignals[0]?.points).toEqual([
      { frequencyHz: 1_000, value: -20 },
      { frequencyHz: 2_000, value: -35 },
      { frequencyHz: 3_000, value: -45 },
    ])
  })

  it('builds a visible x axis from the selected viewport', () => {
    const visibleXAxis = getVisibleSpectrumXAxis(xAxis, {
      startHz: 1_000,
      endHz: 10_000,
    })

    expect(visibleXAxis.minimum).toBe(1_000)
    expect(visibleXAxis.maximum).toBe(10_000)
    expect(visibleXAxis.ticks).toHaveLength(5)
    expect(visibleXAxis.ticks[0]).toBe(1_000)
    expect(visibleXAxis.ticks[4]).toBe(10_000)
  })

  it('returns the nearest spectrum point from ordered points', () => {
    const nearestPoint = getNearestSpectrumPoint(signals[0]?.points ?? [], 2_400)

    expect(nearestPoint).toEqual({ frequencyHz: 2_000, value: -35 })
  })
})
