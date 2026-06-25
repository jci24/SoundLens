export interface ITimeWaveformPoint {
  timeSeconds: number
  minAmplitude: number
  maxAmplitude: number
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
  points: ITimeWaveformPoint[]
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
  failedFiles: string[]
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
  failedFiles: string[]
}
