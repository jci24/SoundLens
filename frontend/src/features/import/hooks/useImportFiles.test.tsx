import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { uploadFiles } from '../services/importFiles'
import { useImportFiles } from './useImportFiles'
import type { IImportFilesResponse } from '../../../common/contracts/import'

vi.mock('../services/importFiles', () => ({
  importFilesByPath: vi.fn(),
  uploadFiles: vi.fn(),
}))

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

describe('useImportFiles', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('exposes a request failure for the dedicated Import page', async () => {
    vi.mocked(uploadFiles).mockRejectedValue(new Error('The upload could not be completed.'))
    const { result } = renderHook(() => useImportFiles())

    await act(async () => {
      await result.current.handleUploadFiles([new File(['audio'], 'test.wav', { type: 'audio/wav' })])
    })

    expect(result.current.importError).toBe('The upload could not be completed.')
    expect(result.current.isImporting).toBe(false)
  })

  it('exposes partial import failures while retaining successful results', async () => {
    vi.mocked(uploadFiles).mockResolvedValue({
      succeededFiles: [
        { fileName: 'baseline.wav', sizeBytes: 5, filePath: '/tmp/baseline.wav', contentType: 'audio/wav' },
      ],
      failedFiles: ['candidate.wav'],
    })
    const { result } = renderHook(() => useImportFiles())

    let response: IImportFilesResponse | undefined
    await act(async () => {
      response = await result.current.handleUploadFiles([new File(['audio'], 'baseline.wav', { type: 'audio/wav' })])
    })

    expect(response?.succeededFiles).toHaveLength(1)
    expect(result.current.importError).toBe('1 file could not be imported.')
  })
})
