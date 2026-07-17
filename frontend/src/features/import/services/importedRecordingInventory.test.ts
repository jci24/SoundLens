import { afterEach, describe, expect, it, vi } from 'vitest'
import { getImportedRecordingInventory } from './importedRecordingInventory'

describe('getImportedRecordingInventory', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('loads backend-owned recording and channel metadata', async () => {
    const inventory = {
      recordings: [{
        recordingId: 'recording-a',
        fileName: 'baseline.wav',
        sizeBytes: 128,
        durationSeconds: 2,
        sampleRate: 48_000,
        channels: 1,
        channelMode: 'mono',
        signals: [{ signalId: 'recording-a:ch:0', channelIndex: 0, displayName: 'Channel 1' }],
      }],
      failedFiles: [],
    }
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => inventory })
    vi.stubGlobal('fetch', fetchMock)

    await expect(getImportedRecordingInventory()).resolves.toEqual(inventory)
    expect(fetchMock).toHaveBeenCalledWith('http://127.0.0.1:5123/api/import/session/recordings')
  })

  it('fails explicitly when inventory cannot be reconstructed', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }))

    await expect(getImportedRecordingInventory()).rejects.toThrow('The recording inventory could not be loaded.')
  })
})
