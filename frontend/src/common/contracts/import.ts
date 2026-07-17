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

export interface IImportFilesByPathRequest {
  filePaths: string[]
}

export interface IImportFilesResponse {
  succeededFiles: IImportedFileSummary[]
  failedFiles: string[]
}
