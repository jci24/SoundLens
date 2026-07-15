import { afterEach, describe, expect, it, vi } from 'vitest'
import { downloadBlobFile, downloadTextFile } from './reportDownload'

describe('downloadTextFile', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('creates a downloadable blob link and clicks it', () => {
    const createObjectUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:report')
    const revokeObjectUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
    const click = vi.fn()
    const createElement = vi.spyOn(document, 'createElement').mockReturnValue({
      click,
      set href(_value: string) {},
      set download(_value: string) {},
    } as unknown as HTMLAnchorElement)

    downloadTextFile('report.md', '# Report')

    expect(createElement).toHaveBeenCalledWith('a')
    expect(createObjectUrl).toHaveBeenCalled()
    expect(click).toHaveBeenCalled()
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:report')
  })

  it('downloads an existing binary blob and revokes its object URL', () => {
    const blob = new Blob(['%PDF-test'], { type: 'application/pdf' })
    const createObjectUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:pdf-report')
    const revokeObjectUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
    const click = vi.fn()
    const anchor = { click } as unknown as HTMLAnchorElement
    const createElement = vi.spyOn(document, 'createElement').mockReturnValue(anchor)

    downloadBlobFile('report.pdf', blob)

    expect(createElement).toHaveBeenCalledWith('a')
    expect(createObjectUrl).toHaveBeenCalledWith(blob)
    expect(anchor.download).toBe('report.pdf')
    expect(anchor.href).toBe('blob:pdf-report')
    expect(click).toHaveBeenCalled()
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:pdf-report')
  })
})
