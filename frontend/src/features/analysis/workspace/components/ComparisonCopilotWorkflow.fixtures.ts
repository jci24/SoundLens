import type { IImportedFileResult } from '../../../../common/contracts/import'
import type { IAgentQueryResponse } from '../../copilot/types/copilot.types'
import type { IRecordingComparisonResponse, ITimeWaveformRecording } from '../../types'

const recordings: ITimeWaveformRecording[] = [
  {
    recordingId: 'recording-a',
    fileName: 'baseline.wav',
    sizeBytes: 1024,
    durationSeconds: 2.5,
    sampleRate: 44100,
    channels: 1,
    channelMode: 'mono',
    signals: [{ signalId: 'signal-a', channelIndex: 0, displayName: 'Channel 1' }],
  },
  {
    recordingId: 'recording-b',
    fileName: 'candidate.wav',
    sizeBytes: 1024,
    durationSeconds: 2.5,
    sampleRate: 44100,
    channels: 1,
    channelMode: 'mono',
    signals: [{ signalId: 'signal-b', channelIndex: 0, displayName: 'Channel 1' }],
  },
]

const importedFiles: IImportedFileResult[] = recordings.map((recording) => ({
  fileName: recording.fileName,
  sizeBytes: recording.sizeBytes,
  contentType: 'audio/wav',
}))

const comparisonResponse: IRecordingComparisonResponse = {
  recordingA: { recordingId: 'recording-a', fileName: 'baseline.wav', channels: 1, durationSeconds: 2.5 },
  recordingB: { recordingId: 'recording-b', fileName: 'candidate.wav', channels: 1, durationSeconds: 2.5 },
  alignedSignals: [{
    signalIdA: 'signal-a',
    displayNameA: 'Channel 1',
    channelIndexA: 0,
    signalIdB: 'signal-b',
    displayNameB: 'Channel 1',
    channelIndexB: 0,
    basis: 'DisplayName',
  }],
  signalObservations: [{
    signalIdA: 'signal-a',
    displayNameA: 'Channel 1',
    channelIndexA: 0,
    signalIdB: 'signal-b',
    displayNameB: 'Channel 1',
    channelIndexB: 0,
    basis: 'DisplayName',
    peakAmplitudeA: 0.8,
    peakAmplitudeB: 0.6,
    peakAmplitudeDelta: 0.2,
    rmsAmplitudeA: 0.5,
    rmsAmplitudeB: 0.3,
    rmsAmplitudeDelta: 0.2,
    crestFactorA: 1.6,
    crestFactorB: 2,
    crestFactorDelta: -0.4,
    clippingSampleCountA: 0,
    clippingSampleCountB: 4,
    clippingSampleCountDelta: -4,
    hasClippingA: false,
    hasClippingB: true,
  }],
  aggregateMetrics: [
    createAggregate('peakAmplitudeDelta', 'FS', 0.2),
    createAggregate('rmsAmplitudeDelta', 'FS', 0.1),
    createAggregate('crestFactorDelta', 'ratio', -0.4),
    createAggregate('clippingSampleCountDelta', 'samples', -4),
  ],
  limitations: [],
  integrityAssessment: {
    status: 'complete',
    limitedCheckCount: 0,
    unknownCheckCount: 1,
    checks: [
      { code: 'SampleRate', status: 'matched', label: 'Sample rate', detail: 'Both recordings use 44,100 Hz.' },
      { code: 'DurationScope', status: 'matched', label: 'Time scope', detail: 'Both recordings use 2.5 s.' },
      { code: 'SignalAlignment', status: 'matched', label: 'Signal alignment', detail: 'All 1 signal pair aligned.' },
      { code: 'Calibration', status: 'unknown', label: 'Calibration', detail: 'No validated calibration is available.' },
    ],
  },
  analysisSpecification: {
    contractVersion: 'comparison-analysis-v1',
    scope: 'full_duration',
    differenceConvention: 'compare_a_minus_compare_b',
    aggregateStatistics: 'mean_median_minimum_maximum_spread',
    metricMethods: [
      { metricKey: 'peakAmplitudeDelta', label: 'Peak amplitude', unit: 'FS', methodId: 'normalized_peak_amplitude', methodVersion: '1', definition: 'Peak definition.' },
      { metricKey: 'rmsAmplitudeDelta', label: 'RMS amplitude', unit: 'FS', methodId: 'normalized_rms_amplitude', methodVersion: '1', definition: 'RMS definition.' },
      { metricKey: 'crestFactorDelta', label: 'Crest factor', unit: 'ratio', methodId: 'peak_to_rms_ratio', methodVersion: '1', definition: 'Crest definition.' },
      { metricKey: 'clippingSampleCountDelta', label: 'Clipping samples', unit: 'samples', methodId: 'decoded_full_scale_sample_count', methodVersion: '1', definition: 'Clipping definition.' },
    ],
  },
  analysisProvenance: {
    contractVersion: 'comparison-provenance-v1',
    recordingA: { algorithm: 'sha256', value: `sha256:${'a'.repeat(64)}` },
    recordingB: { algorithm: 'sha256', value: `sha256:${'b'.repeat(64)}` },
    implementationId: 'soundlens_recording_comparison',
    implementationVersion: '1',
    applicationBuildVersion: '1.0.0-test',
    decoderId: 'soundlens_wav_pcm_ieee_float',
    decoderVersion: '1',
    scope: 'full_duration',
    regionOfInterest: null,
    methods: [
      { methodId: 'normalized_peak_amplitude', methodVersion: '1' },
      { methodId: 'normalized_rms_amplitude', methodVersion: '1' },
      { methodId: 'peak_to_rms_ratio', methodVersion: '1' },
      { methodId: 'decoded_full_scale_sample_count', methodVersion: '1' },
    ],
    parameterFingerprint: `sha256:${'c'.repeat(64)}`,
    evidenceFingerprint: `sha256:${'d'.repeat(64)}`,
    limitations: [
      { code: 'temporary_session', detail: 'Temporary session.' },
      { code: 'incomplete_capture', detail: 'Capture metadata is incomplete.' },
      { code: 'unsigned_manifest', detail: 'Manifest is unsigned.' },
    ],
  },
  regionOfInterest: null,
}

const groundedResponse: IAgentQueryResponse = {
  answer: 'The selected RMS evidence is lower for Compare B.',
  citedEvidence: [{ toolName: 'compare_signals', signalId: 'signal-a', summary: 'Selected comparison evidence' }],
  limitations: ['Answer reflects the selected ROI only.'],
  nextSteps: ['Inspect the selected waveform region.'],
  toolsUsed: ['compare_signals'],
}

function createAggregate(
  metricKey: IRecordingComparisonResponse['aggregateMetrics'][number]['metricKey'],
  unit: string,
  difference: number
) {
  return {
    metricKey,
    unit,
    comparedPairCount: 1,
    missingValueCount: 0,
    meanDifference: difference,
    medianDifference: difference,
    minimumDifference: difference,
    maximumDifference: difference,
    spread: 0,
  }
}

export { comparisonResponse, groundedResponse, importedFiles, recordings }
