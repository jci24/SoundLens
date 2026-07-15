import { describe, expect, it } from 'vitest'
import { getPlaybackRecordingUrl } from './playbackRecording'

describe('getPlaybackRecordingUrl', () => {
  it('builds an encoded recording-ID route without exposing a file path', () => {
    expect(getPlaybackRecordingUrl('recording/id')).toBe(
      'http://127.0.0.1:5123/api/playback/recordings/recording%2Fid'
    )
  })
})
