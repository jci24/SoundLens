import { useMutation } from '@tanstack/react-query'
import { uploadFile } from '../services/uploadService'

export const useUpload = () => {
  return useMutation({
    mutationFn: uploadFile,
    onSuccess: (data) => {
      console.log('Upload successful:', data)
      // Invalidate or update queries if needed
    },
    onError: (error) => {
      console.error('Upload failed:', error)
    },
  })
}
