import { z } from 'zod'

export const IWavMetadataSchema = z.object({
  sampleRate: z.number(),
  bitDepth: z.number(),
  channels: z.number(),
  durationSeconds: z.number(),
  format: z.string(),
  dataSizeBytes: z.number(),
  audioFormat: z.number(),
})

export const IUploadResponseSchema = z.object({
  fileId: z.string(),
  fileName: z.string(),
  fileSize: z.number(),
  contentType: z.string(),
  metadata: IWavMetadataSchema.nullable(),
})

export type IWavMetadata = z.infer<typeof IWavMetadataSchema>
export type IUploadResponse = z.infer<typeof IUploadResponseSchema>
