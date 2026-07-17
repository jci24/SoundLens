import { describe, expect, it } from 'vitest'
import {
  areSignalIdsEqual,
  clampRegionOfInterest,
  clampSpectrumRange,
  getComparisonSetupSummary,
  getNextExpandedRecordings,
  getNextEnabledAnalysisSurfaces,
  getNextRecordingGroupAssignments,
  getNextSingleRecordingGroupAssignment,
  getNextRequestedSignalIds,
  getNextSpectrumRangeEnd,
  getNextSpectrumRangeStart,
  getSwappedRecordingGroupAssignments,
  getWaveformRequestedBinCount,
} from './analysisWorkspaceState'

describe('analysisWorkspaceState', () => {
  it('keeps signal comparison order-sensitive', () => {
    expect(areSignalIdsEqual(['a', 'b'], ['a', 'b'])).toBe(true)
    expect(areSignalIdsEqual(['a', 'b'], ['b', 'a'])).toBe(false)
  })

  it('clamps an ROI to the shared comparison duration', () => {
    expect(clampRegionOfInterest({
      startTimeSeconds: 1.5,
      endTimeSeconds: 2.54,
      durationSeconds: 1.04,
    }, 2.5350113378684807)).toEqual({
      startTimeSeconds: 1.5,
      endTimeSeconds: 2.5350113378684807,
      durationSeconds: 1.0350113378684807,
    })
  })

  it('rejects an ROI that starts at or beyond the shared comparison duration', () => {
    expect(clampRegionOfInterest({
      startTimeSeconds: 2.54,
      endTimeSeconds: 3,
      durationSeconds: 0.46,
    }, 2.5350113378684807)).toBeNull()
  })

  it('toggles expanded recordings', () => {
    expect(getNextExpandedRecordings([], 'recording-1')).toEqual(['recording-1'])
    expect(getNextExpandedRecordings(['recording-1'], 'recording-1')).toEqual([])
  })

  it('keeps analysis selections in domain order and never removes the final method', () => {
    expect(getNextEnabledAnalysisSurfaces(['waveform', 'spectrum'], 'waveform')).toEqual(['spectrum'])
    expect(getNextEnabledAnalysisSurfaces(['spectrum'], 'waveform')).toEqual(['waveform', 'spectrum'])
    expect(getNextEnabledAnalysisSurfaces(['spectrum'], 'spectrum')).toEqual(['spectrum'])
  })

  it('drops recording assignments for recordings that are no longer present', () => {
    expect(
      getNextRecordingGroupAssignments(
        {
          'recording-1': 'A',
          'recording-2': 'unassigned',
          'recording-3': 'B',
        },
        ['recording-1', 'recording-3']
      )
    ).toEqual({
      'recording-1': 'A',
      'recording-3': 'B',
    })
  })

  it('keeps only one recording per compare target when assigning A or B', () => {
    expect(
      getNextSingleRecordingGroupAssignment(
        {
          'recording-1': 'A',
          'recording-2': 'B',
          'recording-3': 'A',
        },
        'recording-2',
        'A'
      )
    ).toEqual({
      'recording-2': 'A',
    })
  })

  it('removes a recording assignment when it is set back to unassigned', () => {
    expect(
      getNextSingleRecordingGroupAssignment(
        {
          'recording-1': 'A',
          'recording-2': 'B',
        },
        'recording-1',
        'unassigned'
      )
    ).toEqual({
      'recording-2': 'B',
    })
  })

  it('swaps compare targets without an intermediate duplicate assignment', () => {
    expect(
      getSwappedRecordingGroupAssignments({
        'recording-1': 'A',
        'recording-2': 'B',
      })
    ).toEqual({
      'recording-1': 'B',
      'recording-2': 'A',
    })
  })

  it('marks multiple recordings on either target as a conflict', () => {
    expect(
      getComparisonSetupSummary(
        [
          {
            recordingId: 'recording-1',
            fileName: 'alpha.wav',
            sizeBytes: 1024,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [],
          },
          {
            recordingId: 'recording-2',
            fileName: 'beta.wav',
            sizeBytes: 1024,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [],
          },
        ],
        { 'recording-1': 'A', 'recording-2': 'A' }
      )
    ).toEqual({
      counts: { A: 2, B: 0, unassigned: 0 },
      state: 'conflict',
    })
  })

  it('marks comparison setup invalid when both groups are empty', () => {
    expect(
      getComparisonSetupSummary(
        [
          {
            recordingId: 'recording-1',
            fileName: 'alpha.wav',
            sizeBytes: 1024,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [],
          },
        ],
        {}
      )
    ).toEqual({
      counts: { unassigned: 1, A: 0, B: 0 },
      state: 'invalid',
    })
  })

  it('marks comparison setup incomplete when only one group has recordings', () => {
    expect(
      getComparisonSetupSummary(
        [
          {
            recordingId: 'recording-1',
            fileName: 'alpha.wav',
            sizeBytes: 1024,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [],
          },
          {
            recordingId: 'recording-2',
            fileName: 'beta.wav',
            sizeBytes: 2048,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [],
          },
        ],
        {
          'recording-1': 'A',
        }
      )
    ).toEqual({
      counts: { unassigned: 1, A: 1, B: 0 },
      state: 'incomplete',
    })
  })

  it('marks comparison setup valid when both groups have recordings', () => {
    expect(
      getComparisonSetupSummary(
        [
          {
            recordingId: 'recording-1',
            fileName: 'alpha.wav',
            sizeBytes: 1024,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [],
          },
          {
            recordingId: 'recording-2',
            fileName: 'beta.wav',
            sizeBytes: 2048,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [],
          },
        ],
        {
          'recording-1': 'A',
          'recording-2': 'B',
        }
      )
    ).toEqual({
      counts: { unassigned: 0, A: 1, B: 1 },
      state: 'valid',
    })
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
