import { describe, expect, it } from 'vitest'
import type { ITimeWaveformRecording } from '../../types'
import { getPlaybackScope, getPositionAlignedTime } from './useRecordingPlayback'

const recording: ITimeWaveformRecording = {
  recordingId: 'recording-1',
  fileName: 'recording.wav',
  sizeBytes: 1_024,
  durationSeconds: 12,
  sampleRate: 44_100,
  channels: 1,
  channelMode: 'Mono',
  signals: [],
}

describe('getPlaybackScope', () => {
  it('uses the full recording when no ROI exists', () => {
    expect(getPlaybackScope(recording, null)).toEqual({
      startTimeSeconds: 0,
      endTimeSeconds: 12,
      hasRegionOfInterest: false,
    })
  })

  it('clamps the ROI to the selected recording duration', () => {
    expect(getPlaybackScope(recording, {
      startTimeSeconds: 10,
      endTimeSeconds: 20,
      durationSeconds: 10,
    })).toEqual({
      startTimeSeconds: 10,
      endTimeSeconds: 12,
      hasRegionOfInterest: true,
    })
  })
})

describe('getPositionAlignedTime', () => {
  it('preserves an in-range position and clamps to the target duration', () => {
    const targetRecording = { ...recording, durationSeconds: 12 }

    expect(getPositionAlignedTime(7, targetRecording, null)).toBe(7)
    expect(getPositionAlignedTime(18, targetRecording, null)).toBe(12)
  })

  it('clamps position to the target ROI', () => {
    const targetRecording = { ...recording, durationSeconds: 30 }
    const region = { startTimeSeconds: 5, endTimeSeconds: 20, durationSeconds: 15 }

    expect(getPositionAlignedTime(2, targetRecording, region)).toBe(5)
    expect(getPositionAlignedTime(11, targetRecording, region)).toBe(11)
    expect(getPositionAlignedTime(24, targetRecording, region)).toBe(20)
  })
})
