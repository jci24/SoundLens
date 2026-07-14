import { afterEach, describe, expect, it, vi } from 'vitest'
import { exportComparisonReportMarkdown, exportReportMarkdown } from './exportReportMarkdown'

describe('exportReportMarkdown', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('posts the current workspace snapshot to the markdown export endpoint', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          fileName: 'soundlens-export-1-recording-20260710-120000.md',
          markdown: '# SoundLens export - 1 recording',
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })
    )

    await exportReportMarkdown({
      activeSurface: 'waveform',
      layoutMode: 'focused',
      signalChartMode: 'overlay',
      recordings: [],
    })

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:5123/api/report/export/markdown',
      expect.objectContaining({
        method: 'POST',
      })
    )
  })

  it('posts only comparison reconstruction identifiers to the comparison endpoint', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ fileName: 'comparison.md', markdown: '# Comparison' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    )

    await exportComparisonReportMarkdown({
      reportTitle: 'Alpha vs beta comparison',
      recordingIdA: 'recording-a',
      recordingIdB: 'recording-b',
      metricKey: 'crestFactorDelta',
      signalIdA: 'signal-a',
      signalIdB: 'signal-b',
      excludedRecordings: [{ recordingId: 'recording-c', assignment: 'unassigned' }],
      startTimeSeconds: 0.2,
      endTimeSeconds: 0.8,
    })

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:5123/api/report/export/comparison/markdown',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          reportTitle: 'Alpha vs beta comparison',
          recordingIdA: 'recording-a',
          recordingIdB: 'recording-b',
          metricKey: 'crestFactorDelta',
          signalIdA: 'signal-a',
          signalIdB: 'signal-b',
          excludedRecordings: [{ recordingId: 'recording-c', assignment: 'unassigned' }],
          startTimeSeconds: 0.2,
          endTimeSeconds: 0.8,
        }),
      })
    )
  })
})
