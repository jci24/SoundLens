import { afterEach, describe, expect, it, vi } from 'vitest'
import { exportComparisonReportPdf, parseDownloadFileName } from './exportComparisonReportPdf'

const request = {
  reportTitle: 'Alpha vs beta comparison',
  recordingIdA: 'recording-a',
  recordingIdB: 'recording-b',
  metricKey: 'crestFactorDelta',
  signalIdA: 'signal-a',
  signalIdB: 'signal-b',
  excludedRecordings: [{ recordingId: 'recording-c', assignment: 'unassigned' as const }],
  startTimeSeconds: 0.2,
  endTimeSeconds: 0.8,
}

describe('exportComparisonReportPdf', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('posts only comparison identifiers and returns the PDF blob and filename', async () => {
    const pdf = new Blob(['%PDF-test'], { type: 'application/pdf' })
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(pdf, {
        status: 200,
        headers: {
          'Content-Type': 'application/pdf',
          'Content-Disposition': "attachment; filename*=UTF-8''alpha-vs-beta.pdf",
        },
      })
    )

    const response = await exportComparisonReportPdf(request)

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:5123/api/report/export/comparison/pdf',
      expect.objectContaining({ method: 'POST', body: JSON.stringify(request) })
    )
    expect(response.fileName).toBe('alpha-vs-beta.pdf')
    expect(response.pdf.type).toBe('application/pdf')
  })

  it('rejects failed responses', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 400 }))

    await expect(exportComparisonReportPdf(request)).rejects.toThrow('could not be prepared')
  })
})

describe('parseDownloadFileName', () => {
  it('supports encoded and quoted filenames', () => {
    expect(parseDownloadFileName("attachment; filename*=UTF-8''comparison%20report.pdf"))
      .toBe('comparison report.pdf')
    expect(parseDownloadFileName('attachment; filename="comparison.pdf"')).toBe('comparison.pdf')
  })

  it('uses a safe fallback for missing, invalid, or path-like filenames', () => {
    expect(parseDownloadFileName(null)).toBe('soundlens-comparison.pdf')
    expect(parseDownloadFileName('attachment; filename="report.txt"')).toBe('soundlens-comparison.pdf')
    expect(parseDownloadFileName('attachment; filename="../../report.pdf"')).toBe('report.pdf')
  })
})
