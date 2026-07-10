import { describe, expect, it, vi, afterEach } from 'vitest'
import { exportReportContext } from './exportReportContext'

describe('exportReportContext', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('posts the current workspace snapshot to the report export endpoint', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          reportTitle: 'SoundLens export - 1 recording',
          exportedAtUtc: '2026-07-10T12:00:00Z',
          activeSurface: 'waveform',
          layoutMode: 'focused',
          signalChartMode: 'overlay',
          regionOfInterest: null,
          recordings: [],
          selectedSignals: [],
          summary: {
            recordingCount: 1,
            totalSignalCount: 2,
            selectedSignalCount: 2,
            hasRegionOfInterest: false,
          },
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })
    )

    await exportReportContext({
      activeSurface: 'waveform',
      layoutMode: 'focused',
      signalChartMode: 'overlay',
      recordings: [],
      selectedSignalIds: ['signal-a'],
    })

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:5123/api/report/export',
      expect.objectContaining({
        method: 'POST',
      })
    )
  })
})
