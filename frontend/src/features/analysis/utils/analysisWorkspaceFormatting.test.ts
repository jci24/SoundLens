import { describe, expect, it } from 'vitest'
import { formatCompactFrequency, formatFrequencyRange } from './analysisWorkspaceFormatting'

describe('analysisWorkspaceFormatting', () => {
  it('formats sub-kilohertz frequencies in hertz', () => {
    expect(formatCompactFrequency(512)).toBe('512 Hz')
  })

  it('formats kilohertz frequencies compactly', () => {
    expect(formatCompactFrequency(5500)).toBe('5.5k Hz')
    expect(formatCompactFrequency(22050)).toBe('22k Hz')
  })

  it('formats frequency ranges using the compact labels', () => {
    expect(formatFrequencyRange(0, 22050)).toBe('0 Hz - 22k Hz')
    expect(formatFrequencyRange(125, 8000)).toBe('125 Hz - 8.0k Hz')
  })
})
