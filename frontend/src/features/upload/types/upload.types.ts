export interface IWavMetadata {
  sampleRate: number
  bitDepth: number
  channels: number
  durationSeconds: number
  format: string
  dataSizeBytes: number
  audioFormat: number
}

export interface IUploadResponse {
  fileId: string
  fileName: string
  fileSize: number
  contentType: string
  metadata: IWavMetadata | null
}
