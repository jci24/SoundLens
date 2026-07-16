import { describe, expect, it } from 'vitest'
import type { ITimeWaveformRecording } from '../../types'
import { buildRecordingRailRows, filterRecordingRailRecordings } from './recordingRailRows'

const recordings: ITimeWaveformRecording[] = [
  {
    recordingId: 'recording-a',
    fileName: 'baseline.wav',
    sizeBytes: 1_024,
    durationSeconds: 1,
    sampleRate: 44_100,
    channels: 2,
    channelMode: 'Stereo',
    signals: [
      { signalId: 'signal-a-left', channelIndex: 0, displayName: 'Left' },
      { signalId: 'signal-a-right', channelIndex: 1, displayName: 'Right' },
    ],
  },
  {
    recordingId: 'recording-b',
    fileName: 'candidate.wav',
    sizeBytes: 2_048,
    durationSeconds: 1,
    sampleRate: 48_000,
    channels: 1,
    channelMode: 'Mono',
    signals: [{ signalId: 'signal-b-mono', channelIndex: 0, displayName: 'Mono reference' }],
  },
]

describe('recordingRailRows', () => {
  it('preserves import and signal order with stable identifier-based keys', () => {
    expect(buildRecordingRailRows(recordings, ['recording-a'])).toEqual([
      expect.objectContaining({ key: 'recording:recording-a', kind: 'recording' }),
      expect.objectContaining({ key: 'signal:signal-a-left', kind: 'signal' }),
      expect.objectContaining({ key: 'signal:signal-a-right', kind: 'signal' }),
      expect.objectContaining({ key: 'recording:recording-b', kind: 'recording' }),
    ])
  })

  it('filters by filename or signal name without changing the expansion state', () => {
    expect(filterRecordingRailRecordings(recordings, 'candidate')).toEqual([recordings[1]])
    expect(filterRecordingRailRecordings(recordings, 'MONO')).toEqual([recordings[1]])
    expect(filterRecordingRailRecordings(recordings, 'missing')).toEqual([])
    expect(filterRecordingRailRecordings(recordings, '  ')).toBe(recordings)
  })
})
