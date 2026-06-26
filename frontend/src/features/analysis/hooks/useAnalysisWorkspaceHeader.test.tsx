import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useAnalysisWorkspaceHeader } from './useAnalysisWorkspaceHeader'

describe('useAnalysisWorkspaceHeader', () => {
  it('returns waveform copy when the waveform surface is active', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceHeader({
        activeSurface: 'waveform',
        layoutMode: 'focused',
        spectrumMaximumHz: 22_050,
        spectrumRangeEndHz: 22_050,
        spectrumRangeStartHz: 0,
        spectrumViewport: null,
      })
    )

    expect(result.current.activeEyebrow).toBe('Time analysis')
    expect(result.current.activeTitle).toBe('Waveform overview')
    expect(result.current.isSpectrumRangeFiltered).toBe(false)
    expect(result.current.spectrumRangeLabel).toBe('0 Hz - 22k Hz')
  })

  it('marks the spectrum range as filtered when the viewport is cropped', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceHeader({
        activeSurface: 'spectrum',
        layoutMode: 'focused',
        spectrumMaximumHz: 22_050,
        spectrumRangeEndHz: 8_000,
        spectrumRangeStartHz: 125,
        spectrumViewport: {
          startHz: 125,
          endHz: 8_000,
        },
      })
    )

    expect(result.current.activeEyebrow).toBe('Frequency analysis')
    expect(result.current.activeTitle).toBe('Spectrum overview')
    expect(result.current.isSpectrumRangeFiltered).toBe(true)
    expect(result.current.spectrumRangeLabel).toBe('125 Hz - 8.0k Hz')
  })

  it('returns multi-surface copy in compare mode', () => {
    const { result } = renderHook(() =>
      useAnalysisWorkspaceHeader({
        activeSurface: 'waveform',
        layoutMode: 'compare',
        spectrumMaximumHz: 22_050,
        spectrumRangeEndHz: 22_050,
        spectrumRangeStartHz: 0,
        spectrumViewport: {
          startHz: 0,
          endHz: 22_050,
        },
      })
    )

    expect(result.current.activeEyebrow).toBe('Multi-surface analysis')
    expect(result.current.activeTitle).toBe('Waveform and spectrum overview')
    expect(result.current.isSpectrumRangeFiltered).toBe(false)
  })
})
