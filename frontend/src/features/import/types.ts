export interface IImportFilesByPathRequest {
  filePaths: string[]
}

export interface IImportedFileSummary {
  fileName: string
  sizeBytes: number
  filePath: string
  contentType: string
}

export interface IImportFilesResponse {
  succeededFiles: IImportedFileSummary[]
  failedFiles: string[]
}

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
