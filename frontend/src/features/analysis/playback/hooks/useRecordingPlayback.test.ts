import { describe, expect, it } from 'vitest'
import type { ITimeWaveformRecording } from '../../types'
import { getPlaybackScope } from './useRecordingPlayback'

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
