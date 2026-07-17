import type {
  IAnalysisRegionOfInterest,
  TAnalysisSurface,
  TComparisonGroupAssignment,
  TComparisonSetupState,
} from '../types'
import type { ITimeWaveformRecording } from '../types'

export interface ISpectrumRange {
  startHz: number
  endHz: number
}

export interface IRequestedRegionOfInterest {
  startTimeSeconds: number
  endTimeSeconds: number
}

export interface IComparisonSetupSummary {
  counts: Record<TComparisonGroupAssignment, number>
  state: TComparisonSetupState
}

const spectrumFftSizeValues = [
  512,
  1024,
  2048,
  4096,
  8192,
  16384,
  32768,
  44100,
] as const

const defaultSpectrumFftSize = 44100
const defaultSpectrumRangeEndHz = 22050
const defaultEnabledAnalysisSurfaces: TAnalysisSurface[] = ['waveform', 'spectrum']

const getWaveformRequestedBinCount = (chartWidth: number) => {
  if (chartWidth <= 0) {
    return 0
  }

  return Math.min(4000, Math.max(64, Math.ceil(chartWidth)))
}

const formatSpectrumFftSizeOption = (fftSize: number) => `FFT size: ${fftSize.toLocaleString()}`
const getOneSidedLineCount = (fftSize: number) => Math.floor(fftSize / 2) + 1

const spectrumFftSizeSelectOptions = spectrumFftSizeValues.map((value) => ({
  label: formatSpectrumFftSizeOption(value),
  value,
}))

// Selection order drives overlay order, so this intentionally compares sequences, not sets.
const areSignalIdsEqual = (left: string[], right: string[]) =>
  left.length === right.length && left.every((signalId, index) => signalId === right[index])

const areRegionOfInterestEqual = (
  left: IRequestedRegionOfInterest | null,
  right: IRequestedRegionOfInterest | null
) => {
  if (left === null && right === null) {
    return true
  }

  if (left === null || right === null) {
    return false
  }

  return left.startTimeSeconds === right.startTimeSeconds && left.endTimeSeconds === right.endTimeSeconds
}

const clampRegionOfInterest = (
  regionOfInterest: IAnalysisRegionOfInterest | null,
  maximumDurationSeconds: number
): IAnalysisRegionOfInterest | null => {
  if (!regionOfInterest || maximumDurationSeconds <= 0) {
    return null
  }

  const startTimeSeconds = Math.min(
    maximumDurationSeconds,
    Math.max(0, regionOfInterest.startTimeSeconds)
  )
  const endTimeSeconds = Math.min(
    maximumDurationSeconds,
    Math.max(startTimeSeconds, regionOfInterest.endTimeSeconds)
  )

  if (endTimeSeconds <= startTimeSeconds) {
    return null
  }

  return {
    startTimeSeconds,
    endTimeSeconds,
    durationSeconds: endTimeSeconds - startTimeSeconds,
  }
}

const getNextExpandedRecordings = (currentExpanded: string[], recordingId: string) =>
  currentExpanded.includes(recordingId)
    ? currentExpanded.filter((expandedPath) => expandedPath !== recordingId)
    : [...currentExpanded, recordingId]

const getNextRecordingGroupAssignments = (
  currentAssignments: Record<string, TComparisonGroupAssignment>,
  availableRecordingIds: string[]
) => {
  const availableIds = new Set(availableRecordingIds)

  return Object.fromEntries(
    Object.entries(currentAssignments).filter(([recordingId]) => availableIds.has(recordingId))
  )
}

const getNextSingleRecordingGroupAssignment = (
  currentAssignments: Record<string, TComparisonGroupAssignment>,
  recordingId: string,
  assignment: TComparisonGroupAssignment
) => {
  const nextAssignments = Object.fromEntries(
    Object.entries(currentAssignments).filter(([candidateRecordingId, candidateAssignment]) => {
      if (candidateRecordingId === recordingId) {
        return false
      }

      if (assignment === 'unassigned') {
        return true
      }

      return candidateAssignment !== assignment
    })
  )

  if (assignment === 'unassigned') {
    return nextAssignments
  }

  return {
    ...nextAssignments,
    [recordingId]: assignment,
  }
}

const getSwappedRecordingGroupAssignments = (
  currentAssignments: Record<string, TComparisonGroupAssignment>
): Record<string, TComparisonGroupAssignment> =>
  Object.fromEntries(
    Object.entries(currentAssignments).map(([recordingId, assignment]) => [
      recordingId,
      assignment === 'A' ? 'B' : assignment === 'B' ? 'A' : assignment,
    ])
  ) as Record<string, TComparisonGroupAssignment>

const getComparisonSetupSummary = (
  recordings: ITimeWaveformRecording[],
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
): IComparisonSetupSummary => {
  const counts = recordings.reduce<Record<TComparisonGroupAssignment, number>>(
    (nextCounts, recording) => {
      const assignment = recordingGroupAssignments[recording.recordingId] ?? 'unassigned'
      nextCounts[assignment] += 1
      return nextCounts
    },
    { unassigned: 0, A: 0, B: 0 }
  )

  if (counts.A > 1 || counts.B > 1) {
    return {
      counts,
      state: 'conflict',
    }
  }

  if (counts.A === 1 && counts.B === 1) {
    return {
      counts,
      state: 'valid',
    }
  }

  if (counts.A > 0 || counts.B > 0) {
    return {
      counts,
      state: 'incomplete',
    }
  }

  return {
    counts,
    state: 'invalid',
  }
}

const getNextRequestedSignalIds = (currentSignalIds: string[], signalId: string) => {
  if (currentSignalIds.length === 0) {
    return [signalId]
  }

  if (currentSignalIds.includes(signalId)) {
    return currentSignalIds.length === 1
      ? currentSignalIds
      : currentSignalIds.filter((currentSignalId) => currentSignalId !== signalId)
  }

  return [...currentSignalIds, signalId]
}

const getNextEnabledAnalysisSurfaces = (
  currentSurfaces: TAnalysisSurface[],
  surface: TAnalysisSurface
): TAnalysisSurface[] => {
  if (!currentSurfaces.includes(surface)) {
    return defaultEnabledAnalysisSurfaces.filter(
      (candidateSurface) => candidateSurface === surface || currentSurfaces.includes(candidateSurface)
    )
  }

  return currentSurfaces.length === 1
    ? currentSurfaces
    : currentSurfaces.filter((candidateSurface) => candidateSurface !== surface)
}

const clampSpectrumRange = (range: ISpectrumRange, maximumHz: number): ISpectrumRange => {
  const startHz = Math.min(Math.max(range.startHz, 0), maximumHz - 1)
  const endHz = Math.max(startHz + 1, Math.min(range.endHz, maximumHz))

  return {
    startHz,
    endHz,
  }
}

const getNextSpectrumRangeStart = (
  parsedValue: number,
  currentRange: ISpectrumRange,
  maximumHz: number
): ISpectrumRange => {
  const nextEndHz = Math.max(1, Math.min(currentRange.endHz, maximumHz))

  return {
    ...currentRange,
    startHz: Math.min(Math.max(parsedValue, 0), nextEndHz - 1),
  }
}

const getNextSpectrumRangeEnd = (
  parsedValue: number,
  currentRange: ISpectrumRange,
  maximumHz: number
): ISpectrumRange => {
  const nextStartHz = Math.min(Math.max(currentRange.startHz, 0), maximumHz - 1)

  return {
    ...currentRange,
    endHz: Math.max(nextStartHz + 1, Math.min(parsedValue, maximumHz)),
  }
}

export {
  areRegionOfInterestEqual,
  areSignalIdsEqual,
  clampRegionOfInterest,
  clampSpectrumRange,
  defaultEnabledAnalysisSurfaces,
  defaultSpectrumFftSize,
  defaultSpectrumRangeEndHz,
  formatSpectrumFftSizeOption,
  getComparisonSetupSummary,
  getNextExpandedRecordings,
  getNextEnabledAnalysisSurfaces,
  getNextRecordingGroupAssignments,
  getNextSingleRecordingGroupAssignment,
  getSwappedRecordingGroupAssignments,
  getNextRequestedSignalIds,
  getNextSpectrumRangeEnd,
  getNextSpectrumRangeStart,
  getOneSidedLineCount,
  getWaveformRequestedBinCount,
  spectrumFftSizeSelectOptions,
}
