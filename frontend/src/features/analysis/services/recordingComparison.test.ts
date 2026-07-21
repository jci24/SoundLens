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
          integrityAssessment: {
            status: 'complete',
            limitedCheckCount: 0,
            unknownCheckCount: 1,
            checks: [
              { code: 'SampleRate', status: 'matched', label: 'Sample rate', detail: 'Both recordings use 44,100 Hz.' },
              { code: 'DurationScope', status: 'matched', label: 'Time scope', detail: 'Both recordings use 1 s.' },
              { code: 'SignalAlignment', status: 'matched', label: 'Signal alignment', detail: 'All signals aligned.' },
              { code: 'Calibration', status: 'unknown', label: 'Calibration', detail: 'Calibration is unknown.' },
            ],
          },
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

  it('rejects malformed backend-owned integrity metadata', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          recordingA: {},
          recordingB: {},
          alignedSignals: [],
          signalObservations: [],
          aggregateMetrics: [],
          limitations: [],
          integrityAssessment: {
            status: 'complete',
            limitedCheckCount: 0,
            unknownCheckCount: 0,
            checks: [{ code: 'InventedCheck', status: 'matched', label: 'Invented', detail: 'Unsafe.' }],
          },
          regionOfInterest: null,
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      )
    )

    await expect(getRecordingComparison('recording-a', 'recording-b')).rejects.toThrow(
      'Comparison results returned an invalid integrity assessment.'
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
