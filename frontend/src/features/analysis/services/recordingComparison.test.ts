import { afterEach, describe, expect, it, vi } from 'vitest'
import { getRecordingComparison } from './recordingComparison'

describe('getRecordingComparison', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('posts the selected recording pair and ROI to the comparison endpoint', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          recordingA: { recordingId: 'a', fileName: 'alpha.wav', channels: 1, durationSeconds: 1 },
          recordingB: { recordingId: 'b', fileName: 'beta.wav', channels: 1, durationSeconds: 1 },
          alignedSignals: [],
          signalObservations: [],
          aggregateMetrics: [],
          limitations: [],
          regionOfInterest: null,
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })
    )

    await getRecordingComparison('recording-a', 'recording-b', {
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
    })

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:5123/api/comparisons/recordings',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          recordingIdA: 'recording-a',
          recordingIdB: 'recording-b',
          startTimeSeconds: 0.1,
          endTimeSeconds: 0.4,
        }),
      })
    )
  })

  it('surfaces nested FastEndpoints comparison errors', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          statusCode: 400,
          message: 'One or more errors occurred!',
          errors: {
            generalErrors: [
              'EndTimeSeconds must be less than or equal to the shortest selected signal duration (2.535 s).',
            ],
          },
        }),
        {
          status: 400,
          headers: { 'Content-Type': 'application/json' },
        }
      )
    )

    await expect(
      getRecordingComparison('recording-a', 'recording-b', {
        startTimeSeconds: 3,
        endTimeSeconds: 3.5,
      })
    ).rejects.toThrow(
      'EndTimeSeconds must be less than or equal to the shortest selected signal duration (2.535 s).'
    )
  })
})
