export type TAnalysisSurface = 'waveform' | 'spectrum'
export type TComparisonGroupAssignment = 'unassigned' | 'A' | 'B'
export type TComparisonSetupState = 'invalid' | 'incomplete' | 'valid'

export interface ISignalFinding {
  category: string
  severity: 'Info' | 'Warning' | 'Alert'
  label: string
  detail?: string
}

export type TAnalysisLayoutMode = 'focused' | 'compare'
export type TSignalChartMode = 'overlay' | 'split'

export type TTimeWaveformBin = [number, number]

export interface IAnalysisRegionOfInterest {
  startTimeSeconds: number
  endTimeSeconds: number
  durationSeconds: number
}

export interface ISignalDerivedMetrics {
  peakAmplitude: number
  rmsAmplitude: number
  crestFactor: number
  clippingSampleCount: number
  hasClipping: boolean
}

export interface ITimeWaveformSignalSummary {
  signalId: string
  channelIndex: number
  displayName: string
}

export interface ITimeWaveformRecording {
  recordingId: string
  fileName: string
  sizeBytes: number
  durationSeconds: number
  sampleRate: number
  channels: number
  channelMode: string
  signals: ITimeWaveformSignalSummary[]
}

export interface ITimeWaveformSignal {
  signalId: string
  recordingId: string
  recordingFileName: string
  displayName: string
  durationSeconds: number
  sampleRate: number
  channelIndex: number
  amplitudeUnit: string
  isCalibrated: boolean
  metrics?: ISignalDerivedMetrics
  findings: ISignalFinding[]
  bins: TTimeWaveformBin[]
}

export interface ITimeWaveformAxis {
  unit: string
  minimum: number
  maximum: number
  ticks: number[]
}

export interface ITimeWaveformResponse {
  requestedBinCount: number
  recordings: ITimeWaveformRecording[]
  selectedSignals: ITimeWaveformSignal[]
  yAxis: ITimeWaveformAxis
  regionOfInterest: IAnalysisRegionOfInterest | null
  failedFiles: string[]
}

export interface IRecordingComparisonSignalPair {
  signalIdA: string
  displayNameA: string
  channelIndexA: number
  signalIdB: string
  displayNameB: string
  channelIndexB: number
  basis: 'None' | 'DisplayName' | 'ChannelIndex'
}

export interface IRecordingComparisonSignalObservation {
  signalIdA: string
  displayNameA: string
  channelIndexA: number
  signalIdB: string
  displayNameB: string
  channelIndexB: number
  basis: 'None' | 'DisplayName' | 'ChannelIndex'
  peakAmplitudeA: number
  peakAmplitudeB: number
  peakAmplitudeDelta: number
  rmsAmplitudeA: number
  rmsAmplitudeB: number
  rmsAmplitudeDelta: number
  crestFactorA: number
  crestFactorB: number
  crestFactorDelta: number
  clippingSampleCountA: number
  clippingSampleCountB: number
  clippingSampleCountDelta: number
  hasClippingA: boolean
  hasClippingB: boolean
}

export interface IRecordingComparisonMetricAggregate {
  metricKey: 'peakAmplitudeDelta' | 'rmsAmplitudeDelta' | 'crestFactorDelta' | 'clippingSampleCountDelta'
  unit: string
  comparedPairCount: number
  missingValueCount: number
  meanDifference: number
  medianDifference: number
  minimumDifference: number
  maximumDifference: number
  spread: number
}

export interface IRecordingComparisonLimitation {
  code: string
  detail: string
}

export interface IRecordingComparisonRecord {
  recordingId: string
  fileName: string
  channels: number
  durationSeconds: number
}

export interface IRecordingComparisonResponse {
  recordingA: IRecordingComparisonRecord
  recordingB: IRecordingComparisonRecord
  alignedSignals: IRecordingComparisonSignalPair[]
  signalObservations: IRecordingComparisonSignalObservation[]
  aggregateMetrics: IRecordingComparisonMetricAggregate[]
  limitations: IRecordingComparisonLimitation[]
  regionOfInterest: IAnalysisRegionOfInterest | null
}

export interface IComparisonCopilotObservation {
  signalIdA: string
  displayNameA: string
  signalIdB: string
  displayNameB: string
  valueA: number
  valueB: number
  delta: number
}

export interface IComparisonCopilotFinding {
  signalId: string
  label: string
  detail?: string
}

export interface IComparisonCopilotContext {
  recordingIdA: string
  recordingFileNameA: string
  recordingIdB: string
  recordingFileNameB: string
  metricKey: IRecordingComparisonMetricAggregate['metricKey']
  metricLabel: string
  unit: string
  comparedPairCount: number
  missingValueCount: number
  meanDifference: number
  medianDifference: number
  spread: number
  coverageLabel: string
  coverageCopy: string
  limitations: IRecordingComparisonLimitation[]
  observation: IComparisonCopilotObservation
  findings: IComparisonCopilotFinding[]
}

export interface IFrequencySpectrumPoint {
  frequencyHz: number
  value: number
}

export interface IFrequencySpectrumSignal {
  signalId: string
  recordingId: string
  recordingFileName: string
  displayName: string
  durationSeconds: number
  sampleRate: number
  channelIndex: number
  amplitudeUnit: string
  isCalibrated: boolean
  metrics?: ISignalDerivedMetrics
  findings: ISignalFinding[]
  points: IFrequencySpectrumPoint[]
}

export interface IFrequencySpectrumAxis {
  unit: string
  minimum: number
  maximum: number
  ticks: number[]
}

export interface IFrequencySpectrumAnalysis {
  method: string
  window: string
  overlapPercent: number
  fftLength: number
  frequencyResolutionHz: number
  averagingMode: string
  spectrumType: string
  amplitudeUnit: string
  isCalibrated: boolean
}

export interface IFrequencySpectrumResponse {
  requestedBinCount: number
  recordings: ITimeWaveformRecording[]
  selectedSignals: IFrequencySpectrumSignal[]
  xAxis: IFrequencySpectrumAxis
  yAxis: IFrequencySpectrumAxis
  analysis: IFrequencySpectrumAnalysis
  regionOfInterest: IAnalysisRegionOfInterest | null
  failedFiles: string[]
}
