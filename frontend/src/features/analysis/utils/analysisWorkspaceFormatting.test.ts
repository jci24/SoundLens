import { describe, expect, it } from 'vitest'
import {
  formatAmplitude,
  formatClippingState,
  formatCompactDuration,
  formatCompactFrequency,
  formatCompactSampleRate,
  formatCrestFactor,
  formatFrequencyRange,
} from './analysisWorkspaceFormatting'

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

  it('formats derived metric values compactly', () => {
    expect(formatAmplitude(0.81234)).toBe('0.812 FS')
    expect(formatCrestFactor(1.4142)).toBe('1.41')
    expect(formatCompactDuration(2)).toBe('2.00 s')
    expect(formatCompactDuration(12.4)).toBe('12.4 s')
    expect(formatCompactSampleRate(44100)).toBe('44.1k Hz')
    expect(formatClippingState(0)).toBe('Clean')
    expect(formatClippingState(3)).toBe('3 samp')
  })
})
