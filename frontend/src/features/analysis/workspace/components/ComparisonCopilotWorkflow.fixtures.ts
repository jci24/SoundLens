import type { IImportedFileSummary } from '../../../../common/contracts/import'
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

const importedFiles: IImportedFileSummary[] = recordings.map((recording) => ({
  fileName: recording.fileName,
  sizeBytes: recording.sizeBytes,
  filePath: `/tmp/${recording.fileName}`,
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
