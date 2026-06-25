export interface IImportedFileSummary {
  fileName: string
  sizeBytes: number
  filePath: string
  contentType: string
}

export interface IImportFilesByPathRequest {
  filePaths: string[]
}

export interface IImportFilesResponse {
  succeededFiles: IImportedFileSummary[]
  failedFiles: string[]
}
