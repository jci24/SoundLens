import { afterEach, describe, expect, it, vi } from 'vitest'
import { getRecordingComparison } from './recordingComparison'

const analysisSpecification = {
  contractVersion: 'comparison-analysis-v1',
  scope: 'roi',
  differenceConvention: 'compare_a_minus_compare_b',
  aggregateStatistics: 'mean_median_minimum_maximum_spread',
  metricMethods: [
    { metricKey: 'peakAmplitudeDelta', label: 'Peak amplitude', unit: 'FS', methodId: 'normalized_peak_amplitude', methodVersion: '1', definition: 'Peak definition.' },
    { metricKey: 'rmsAmplitudeDelta', label: 'RMS amplitude', unit: 'FS', methodId: 'normalized_rms_amplitude', methodVersion: '1', definition: 'RMS definition.' },
    { metricKey: 'crestFactorDelta', label: 'Crest factor', unit: 'ratio', methodId: 'peak_to_rms_ratio', methodVersion: '1', definition: 'Crest definition.' },
    { metricKey: 'clippingSampleCountDelta', label: 'Clipping samples', unit: 'samples', methodId: 'decoded_full_scale_sample_count', methodVersion: '1', definition: 'Clipping definition.' },
  ],
}

const analysisProvenance = {
  contractVersion: 'comparison-provenance-v1',
  recordingA: { algorithm: 'sha256', value: `sha256:${'a'.repeat(64)}` },
  recordingB: { algorithm: 'sha256', value: `sha256:${'b'.repeat(64)}` },
  implementationId: 'soundlens_recording_comparison',
  implementationVersion: '1',
  applicationBuildVersion: '1.0.0-test',
  decoderId: 'soundlens_wav_pcm_ieee_float',
  decoderVersion: '1',
  scope: 'roi',
  regionOfInterest: { startTimeSeconds: 0.1, endTimeSeconds: 0.4, durationSeconds: 0.3 },
  methods: analysisSpecification.metricMethods.map(({ methodId, methodVersion }) => ({ methodId, methodVersion })),
  parameterFingerprint: `sha256:${'c'.repeat(64)}`,
  evidenceFingerprint: `sha256:${'d'.repeat(64)}`,
  limitations: [
    { code: 'temporary_session', detail: 'Temporary session.' },
    { code: 'incomplete_capture', detail: 'Capture metadata is incomplete.' },
    { code: 'unsigned_manifest', detail: 'Manifest is unsigned.' },
  ],
}

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
          aggregateMetrics: analysisSpecification.metricMethods.map(({ metricKey, unit }) => ({ metricKey, unit })),
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
          analysisSpecification,
          analysisProvenance,
          regionOfInterest: { startTimeSeconds: 0.1, endTimeSeconds: 0.4, durationSeconds: 0.3 },
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

  it('rejects malformed backend-owned analysis specifications', async () => {
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
            unknownCheckCount: 1,
            checks: [
              { code: 'SampleRate', status: 'matched', label: 'Sample rate', detail: 'Matched.' },
              { code: 'DurationScope', status: 'matched', label: 'Time scope', detail: 'Matched.' },
              { code: 'SignalAlignment', status: 'matched', label: 'Signal alignment', detail: 'Matched.' },
              { code: 'Calibration', status: 'unknown', label: 'Calibration', detail: 'Unknown.' },
            ],
          },
          analysisSpecification: {
            ...analysisSpecification,
            metricMethods: [...analysisSpecification.metricMethods].reverse(),
          },
          regionOfInterest: null,
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      )
    )

    await expect(getRecordingComparison('recording-a', 'recording-b')).rejects.toThrow(
      'Comparison results returned an invalid analysis specification.'
    )
  })

  it('rejects an ROI analysis specification without a valid ROI', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          recordingA: {},
          recordingB: {},
          alignedSignals: [],
          signalObservations: [],
          aggregateMetrics: analysisSpecification.metricMethods.map(({ metricKey, unit }) => ({ metricKey, unit })),
          limitations: [],
          integrityAssessment: {
            status: 'complete',
            limitedCheckCount: 0,
            unknownCheckCount: 1,
            checks: [
              { code: 'SampleRate', status: 'matched', label: 'Sample rate', detail: 'Matched.' },
              { code: 'DurationScope', status: 'matched', label: 'Time scope', detail: 'Matched.' },
              { code: 'SignalAlignment', status: 'matched', label: 'Signal alignment', detail: 'Matched.' },
              { code: 'Calibration', status: 'unknown', label: 'Calibration', detail: 'Unknown.' },
            ],
          },
          analysisSpecification,
          regionOfInterest: undefined,
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      )
    )

    await expect(getRecordingComparison('recording-a', 'recording-b')).rejects.toThrow(
      'Comparison results returned an invalid analysis specification.'
    )
  })

  it('rejects malformed backend-owned analysis provenance', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          recordingA: {},
          recordingB: {},
          alignedSignals: [],
          signalObservations: [],
          aggregateMetrics: analysisSpecification.metricMethods.map(({ metricKey, unit }) => ({ metricKey, unit })),
          limitations: [],
          integrityAssessment: {
            status: 'complete',
            limitedCheckCount: 0,
            unknownCheckCount: 1,
            checks: [
              { code: 'SampleRate', status: 'matched', label: 'Sample rate', detail: 'Matched.' },
              { code: 'DurationScope', status: 'matched', label: 'Time scope', detail: 'Matched.' },
              { code: 'SignalAlignment', status: 'matched', label: 'Signal alignment', detail: 'Matched.' },
              { code: 'Calibration', status: 'unknown', label: 'Calibration', detail: 'Unknown.' },
            ],
          },
          analysisSpecification,
          analysisProvenance: {
            ...analysisProvenance,
            evidenceFingerprint: 'sha256:not-a-valid-fingerprint',
          },
          regionOfInterest: { startTimeSeconds: 0.1, endTimeSeconds: 0.4, durationSeconds: 0.3 },
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      )
    )

    await expect(getRecordingComparison('recording-a', 'recording-b')).rejects.toThrow(
      'Comparison results returned invalid analysis provenance.'
    )
  })

  it('rejects unknown, reordered, and scope-inconsistent provenance', async () => {
    const invalidProvenance = [
      { ...analysisProvenance, implementationId: 'invented_implementation' },
      { ...analysisProvenance, methods: [...analysisProvenance.methods].reverse() },
      { ...analysisProvenance, scope: 'full_duration', regionOfInterest: null },
    ]
    const fetchMock = vi.spyOn(globalThis, 'fetch')

    for (const provenance of invalidProvenance) {
      fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({
        recordingA: {},
        recordingB: {},
        alignedSignals: [],
        signalObservations: [],
        aggregateMetrics: analysisSpecification.metricMethods.map(({ metricKey, unit }) => ({ metricKey, unit })),
        limitations: [],
        integrityAssessment: {
          status: 'complete',
          limitedCheckCount: 0,
          unknownCheckCount: 1,
          checks: [
            { code: 'SampleRate', status: 'matched', label: 'Sample rate', detail: 'Matched.' },
            { code: 'DurationScope', status: 'matched', label: 'Time scope', detail: 'Matched.' },
            { code: 'SignalAlignment', status: 'matched', label: 'Signal alignment', detail: 'Matched.' },
            { code: 'Calibration', status: 'unknown', label: 'Calibration', detail: 'Unknown.' },
          ],
        },
        analysisSpecification,
        analysisProvenance: provenance,
        regionOfInterest: { startTimeSeconds: 0.1, endTimeSeconds: 0.4, durationSeconds: 0.3 },
      }), { status: 200, headers: { 'Content-Type': 'application/json' } }))

      await expect(getRecordingComparison('recording-a', 'recording-b')).rejects.toThrow(
        'Comparison results returned invalid analysis provenance.'
      )
    }
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
