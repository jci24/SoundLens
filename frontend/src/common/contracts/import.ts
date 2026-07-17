export interface IImportedFileSummary {
  fileName: string
  sizeBytes: number
  filePath: string
  contentType: string
}

export interface IImportSessionFileSummary {
  fileName: string
  sizeBytes: number
  contentType: string
}

export interface IImportSessionResponse {
  files: IImportSessionFileSummary[]
}

export interface IImportedRecordingSignal {
  signalId: string
  channelIndex: number
  displayName: string
}

export interface IImportedRecordingInventoryItem {
  recordingId: string
  fileName: string
  sizeBytes: number
  durationSeconds: number
  sampleRate: number
  channels: number
  channelMode: string
  signals: IImportedRecordingSignal[]
}

export interface IImportedRecordingInventoryResponse {
  recordings: IImportedRecordingInventoryItem[]
  failedFiles: string[]
}

export interface IImportFilesByPathRequest {
  filePaths: string[]
}

export interface IImportFilesResponse {
  succeededFiles: IImportedFileSummary[]
  failedFiles: string[]
}
