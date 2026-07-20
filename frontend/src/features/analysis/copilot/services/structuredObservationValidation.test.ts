import { describe, expect, it } from 'vitest'
import { isStructuredObservationCollection } from './structuredObservationValidation'

const buildMetricObservation = () => ({
  observationId: 'obs_v1_111111111111111111111111',
  kind: 'comparison_metric',
  status: 'complete',
  scope: { kind: 'full_duration', startTimeSeconds: null, endTimeSeconds: null },
  limitationCodes: [],
  evidenceReferences: [
    {
      referenceId: 'obs_v1_111111111111111111111111',
      evidenceType: 'comparison_metric',
      recordingIds: ['recording-a', 'recording-b'],
      signalIds: ['recording-a:ch:0', 'recording-b:ch:0'],
      metricKey: 'rmsAmplitudeDelta',
      scope: { kind: 'full_duration', startTimeSeconds: null, endTimeSeconds: null },
    },
  ],
  comparisonMetric: {
    metricKey: 'rmsAmplitudeDelta',
    metricLabel: 'RMS amplitude',
    unit: 'FS',
    aggregate: {
      comparedPairCount: 2,
      missingValueCount: 0,
      meanDifference: -0.1,
      medianDifference: -0.1,
      minimumDifference: -0.2,
      maximumDifference: 0,
      spread: 0.2,
    },
    selectedPair: {
      recordingIdA: 'recording-a',
      recordingFileNameA: 'a.wav',
      signalIdA: 'recording-a:ch:0',
      signalDisplayNameA: 'Channel 1',
      valueA: 0.2,
      recordingIdB: 'recording-b',
      recordingFileNameB: 'b.wav',
      signalIdB: 'recording-b:ch:0',
      signalDisplayNameB: 'Channel 1',
      valueB: 0.3,
      difference: -0.1,
    },
  },
  signalFinding: null,
})

describe('isStructuredObservationCollection', () => {
  it('accepts an absent or valid additive observation collection', () => {
    expect(isStructuredObservationCollection(undefined)).toBe(true)
    expect(isStructuredObservationCollection([buildMetricObservation()])).toBe(true)
  })

  it('rejects duplicate and mismatched references', () => {
    const observation = buildMetricObservation()
    expect(isStructuredObservationCollection([observation, observation])).toBe(false)
    expect(isStructuredObservationCollection([
      {
        ...observation,
        evidenceReferences: [{ ...observation.evidenceReferences[0], referenceId: 'stale-reference' }],
      },
    ])).toBe(false)
    expect(isStructuredObservationCollection([
      {
        ...observation,
        evidenceReferences: [{ ...observation.evidenceReferences[0], metricKey: 'crestFactorDelta' }],
      },
    ])).toBe(false)
    expect(isStructuredObservationCollection([
      {
        ...observation,
        evidenceReferences: [{ ...observation.evidenceReferences[0], signalIds: ['other-a', 'other-b'] }],
      },
    ])).toBe(false)
  })

  it('rejects invalid status, scope, and metric-unit combinations', () => {
    const observation = buildMetricObservation()
    expect(isStructuredObservationCollection([{ ...observation, status: 'supported' }])).toBe(false)
    expect(isStructuredObservationCollection([
      { ...observation, scope: { kind: 'roi', startTimeSeconds: 1, endTimeSeconds: 0.5 } },
    ])).toBe(false)
    expect(isStructuredObservationCollection([
      {
        ...observation,
        comparisonMetric: { ...observation.comparisonMetric, unit: 'dB SPL' },
      },
    ])).toBe(false)
  })

  it('rejects payload-kind mismatches and non-finite measurements', () => {
    const observation = buildMetricObservation()
    expect(isStructuredObservationCollection([
      { ...observation, signalFinding: { label: 'Unexpected' } },
    ])).toBe(false)
    expect(isStructuredObservationCollection([
      {
        ...observation,
        comparisonMetric: {
          ...observation.comparisonMetric,
          aggregate: { ...observation.comparisonMetric.aggregate, meanDifference: Number.NaN },
        },
      },
    ])).toBe(false)
  })

  it('accepts a complete typed finding and rejects mixed finding status', () => {
    const metric = buildMetricObservation()
    const finding = {
      observationId: 'obs_v1_222222222222222222222222',
      kind: 'signal_finding',
      status: 'complete',
      scope: metric.scope,
      limitationCodes: [],
      evidenceReferences: [
        {
          referenceId: 'obs_v1_222222222222222222222222',
          evidenceType: 'signal_finding',
          recordingIds: ['recording-a'],
          signalIds: ['recording-a:ch:0'],
          metricKey: null,
          scope: metric.scope,
        },
      ],
      comparisonMetric: null,
      signalFinding: {
        side: 'A',
        recordingId: 'recording-a',
        recordingFileName: 'a.wav',
        signalId: 'recording-a:ch:0',
        signalDisplayName: 'Channel 1',
        category: 'TonalPeak',
        severity: 'Info',
        label: 'Dominant tonal component',
        detail: null,
      },
    }

    expect(isStructuredObservationCollection([finding])).toBe(true)
    expect(isStructuredObservationCollection([{ ...finding, status: 'mixed' }])).toBe(false)
  })
})
