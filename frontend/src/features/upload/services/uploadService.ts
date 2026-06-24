import { IUploadResponseSchema, type IUploadResponse } from '../../../common/api/upload.schema'

const API_BASE_URL = 'http://localhost:5000/api'

export const uploadFile = async (file: File): Promise<IUploadResponse> => {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${API_BASE_URL}/files`, {
    method: 'POST',
    body: formData,
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(errorText || 'Upload failed')
  }

  const data = await response.json()
  return IUploadResponseSchema.parse(data)
}
