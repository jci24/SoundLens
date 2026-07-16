import type { ITimeWaveformRecording, ITimeWaveformSignalSummary } from '../../types'

interface IRecordingRailRecordingRow {
  key: string
  kind: 'recording'
  recording: ITimeWaveformRecording
}

interface IRecordingRailSignalRow {
  key: string
  kind: 'signal'
  recordingId: string
  signal: ITimeWaveformSignalSummary
}

type TRecordingRailRow = IRecordingRailRecordingRow | IRecordingRailSignalRow

const normalizeSearchQuery = (query: string) => query.trim().toLocaleLowerCase()

const filterRecordingRailRecordings = (
  recordings: ITimeWaveformRecording[],
  query: string
) => {
  const normalizedQuery = normalizeSearchQuery(query)

  if (!normalizedQuery) {
    return recordings
  }

  return recordings.filter((recording) =>
    recording.fileName.toLocaleLowerCase().includes(normalizedQuery) ||
    recording.signals.some((signal) => signal.displayName.toLocaleLowerCase().includes(normalizedQuery))
  )
}

const buildRecordingRailRows = (
  recordings: ITimeWaveformRecording[],
  expandedRecordings: string[]
): TRecordingRailRow[] => recordings.flatMap((recording) => {
  const recordingRow: IRecordingRailRecordingRow = {
    key: `recording:${recording.recordingId}`,
    kind: 'recording',
    recording,
  }

  if (!expandedRecordings.includes(recording.recordingId)) {
    return [recordingRow]
  }

  return [
    recordingRow,
    ...recording.signals.map<IRecordingRailSignalRow>((signal) => ({
      key: `signal:${signal.signalId}`,
      kind: 'signal',
      recordingId: recording.recordingId,
      signal,
    })),
  ]
})

export {
  buildRecordingRailRows,
  filterRecordingRailRecordings,
  type TRecordingRailRow,
}
