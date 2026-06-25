import { describe, expect, it } from 'vitest'
import {
  areSignalIdsEqual,
  clampSpectrumRange,
  getNextExpandedRecordings,
  getNextRequestedSignalIds,
  getNextSpectrumRangeEnd,
  getNextSpectrumRangeStart,
  getWaveformRequestedBinCount,
} from './analysisWorkspaceState'

describe('analysisWorkspaceState', () => {
  it('keeps signal comparison order-sensitive', () => {
    expect(areSignalIdsEqual(['a', 'b'], ['a', 'b'])).toBe(true)
    expect(areSignalIdsEqual(['a', 'b'], ['b', 'a'])).toBe(false)
  })

  it('toggles expanded recordings', () => {
    expect(getNextExpandedRecordings([], 'recording-1')).toEqual(['recording-1'])
    expect(getNextExpandedRecordings(['recording-1'], 'recording-1')).toEqual([])
  })

  it('keeps at least one selected signal', () => {
    expect(getNextRequestedSignalIds([], 'signal-1')).toEqual(['signal-1'])
    expect(getNextRequestedSignalIds(['signal-1'], 'signal-1')).toEqual(['signal-1'])
    expect(getNextRequestedSignalIds(['signal-1', 'signal-2'], 'signal-1')).toEqual(['signal-2'])
  })

  it('adds additional selected signals for comparison', () => {
    expect(getNextRequestedSignalIds(['signal-1'], 'signal-2')).toEqual(['signal-1', 'signal-2'])
  })

  it('clamps the visible spectrum range inside the available maximum', () => {
    expect(clampSpectrumRange({ startHz: -10, endHz: 40_000 }, 22_050)).toEqual({
      startHz: 0,
      endHz: 22_050,
    })
  })

  it('clamps the start range below the current end', () => {
    expect(getNextSpectrumRangeStart(21_000, { startHz: 0, endHz: 20_000 }, 22_050)).toEqual({
      startHz: 19_999,
      endHz: 20_000,
    })
  })

  it('clamps the end range above the current start', () => {
    expect(getNextSpectrumRangeEnd(500, { startHz: 1_000, endHz: 22_050 }, 22_050)).toEqual({
      startHz: 1_000,
      endHz: 1_001,
    })
  })

  it('requests one waveform bin per visible pixel without retina doubling', () => {
    expect(getWaveformRequestedBinCount(0)).toBe(0)
    expect(getWaveformRequestedBinCount(40)).toBe(64)
    expect(getWaveformRequestedBinCount(912.2)).toBe(913)
    expect(getWaveformRequestedBinCount(5000)).toBe(4000)
  })
})
