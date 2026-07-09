import { describe, expect, it } from 'vitest'
import { buildWaveformPath } from './waveformChart'
import type { ITimeWaveformSignal } from '../../types'

const signal: ITimeWaveformSignal = {
  signalId: 'signal-1',
  recordingId: 'recording-1',
  recordingFileName: 'recording.wav',
  displayName: 'Channel 1',
  durationSeconds: 1,
  sampleRate: 44_100,
  channelIndex: 0,
  amplitudeUnit: 'FS',
  isCalibrated: false,
  metrics: {
    peakAmplitude: 0.9,
    rmsAmplitude: 0.5,
    crestFactor: 1.8,
    clippingSampleCount: 0,
    hasClipping: false,
  },
  findings: [],
  bins: [
    [-0.5, 0.25],
    [-0.25, 0.75],
    [-0.75, 0.5],
  ],
}

describe('waveformChart', () => {
  it('builds a single SVG path with move-line segments for each waveform bin', () => {
    const path = buildWaveformPath(
      signal,
      (binIndex, binCount) => (binCount <= 1 ? 0 : binIndex * 10),
      (amplitude) => amplitude * 100
    )

    expect(path).toBe('M0 -50 L0 25 M10 -25 L10 75 M20 -75 L20 50')
  })
})
