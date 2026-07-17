import { afterEach, describe, expect, it, vi } from 'vitest'
import { getCurrentImportSession } from './currentImportSession'

describe('getCurrentImportSession', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns the safe ordered session metadata', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        files: [
          { fileName: 'baseline.wav', sizeBytes: 123, contentType: 'audio/wav' },
          { fileName: 'candidate.wav', sizeBytes: 456, contentType: 'audio/wav' },
        ],
      }),
    })
    vi.stubGlobal('fetch', fetchMock)

    await expect(getCurrentImportSession()).resolves.toEqual({
      files: [
        { fileName: 'baseline.wav', sizeBytes: 123, contentType: 'audio/wav' },
        { fileName: 'candidate.wav', sizeBytes: 456, contentType: 'audio/wav' },
      ],
    })
    expect(fetchMock).toHaveBeenCalledWith('http://127.0.0.1:5123/api/import/session')
  })

  it('throws an explicit restoration error for a failed request', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }))

    await expect(getCurrentImportSession()).rejects.toThrow('The current import session could not be restored.')
  })
})
