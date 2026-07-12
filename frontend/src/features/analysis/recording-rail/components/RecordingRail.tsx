import { ChevronRight } from 'lucide-react'
import { Checkbox } from '@/components/ui/checkbox'
import { Field, FieldLabel } from '@/components/ui/field'
import type { ITimeWaveformRecording, TComparisonGroupAssignment } from '../../types'
import './RecordingRail.scss'

interface IRecordingRailProps {
  expandedRecordings: string[]
  onRecordingGroupAssignment: (recordingId: string, assignment: TComparisonGroupAssignment) => void
  onRecordingToggle: (recordingId: string) => void
  onSignalSelection: (signalId: string) => void
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  selectedSignalIds: string[]
}

const assignmentLabels: Record<TComparisonGroupAssignment, string> = {
  unassigned: 'Unassigned',
  A: 'Group A',
  B: 'Group B',
}

const assignmentBadgeLabels: Record<TComparisonGroupAssignment, string> = {
  unassigned: 'Unassigned',
  A: 'A',
  B: 'B',
}

const RecordingRail = ({
  expandedRecordings,
  onRecordingGroupAssignment,
  onRecordingToggle,
  onSignalSelection,
  recordings,
  recordingGroupAssignments,
  selectedSignalIds,
}: IRecordingRailProps) => (
  <aside className="time-waveform-workspace__recording-rail" aria-label="Imported recordings and channels">
    <div className="time-waveform-workspace__recording-rail-header">
      <span className="time-waveform-workspace__recording-rail-title">Comparison inputs</span>
      <span className="time-waveform-workspace__recording-rail-hint">Open a recording to assign it to A or B.</span>
    </div>

    <div className="time-waveform-workspace__recording-list">
      {recordings.map((recording) => (
        <section
          className={`time-waveform-workspace__recording-group${expandedRecordings.includes(recording.recordingId) ? ' time-waveform-workspace__recording-group--expanded' : ''}`}
          key={recording.recordingId}
        >
          <div className="time-waveform-workspace__recording-group-header">
            <button
              className="time-waveform-workspace__recording-toggle"
              type="button"
              onClick={() => onRecordingToggle(recording.recordingId)}
            >
              <span className="time-waveform-workspace__recording-heading">
                <ChevronRight
                  className={`time-waveform-workspace__recording-chevron${expandedRecordings.includes(recording.recordingId) ? ' time-waveform-workspace__recording-chevron--open' : ''}`}
                  size={14}
                />
                <span className="time-waveform-workspace__recording-name">{recording.fileName}</span>
              </span>
            </button>
            <span
              className={`time-waveform-workspace__recording-assignment-badge time-waveform-workspace__recording-assignment-badge--${recordingGroupAssignments[recording.recordingId] ?? 'unassigned'}`}
              title={assignmentLabels[recordingGroupAssignments[recording.recordingId] ?? 'unassigned']}
            >
              {assignmentBadgeLabels[recordingGroupAssignments[recording.recordingId] ?? 'unassigned']}
            </span>
          </div>

          {expandedRecordings.includes(recording.recordingId) && (
            <div className="time-waveform-workspace__recording-detail">
              <div className="time-waveform-workspace__assignment-panel">
                <span className="time-waveform-workspace__assignment-caption">Assignment</span>
                <div
                  aria-label={`${recording.fileName} comparison group`}
                  className="time-waveform-workspace__assignment-picker"
                  role="group"
                >
                  {(['A', 'B', 'unassigned'] as const).map((assignment) => {
                    const isActive = (recordingGroupAssignments[recording.recordingId] ?? 'unassigned') === assignment

                    return (
                      <button
                        key={assignment}
                        aria-pressed={isActive}
                        className={`time-waveform-workspace__assignment-button${isActive ? ' time-waveform-workspace__assignment-button--active' : ''}`}
                        type="button"
                        onClick={() => onRecordingGroupAssignment(recording.recordingId, assignment)}
                      >
                        {assignmentLabels[assignment]}
                      </button>
                    )
                  })}
                </div>
              </div>

              <div className="time-waveform-workspace__signal-list">
                <span className="time-waveform-workspace__signal-list-caption">Signals</span>
                {recording.signals.map((signal) => {
                  const isSelected = selectedSignalIds.includes(signal.signalId)

                  return (
                    <Field
                      className={`time-waveform-workspace__signal-row${isSelected ? ' time-waveform-workspace__signal-row--selected' : ''}`}
                      data-selected={isSelected}
                      key={signal.signalId}
                      orientation="horizontal"
                    >
                      <Checkbox
                        checked={isSelected}
                        className="time-waveform-workspace__signal-checkbox"
                        id={signal.signalId}
                        onCheckedChange={() => onSignalSelection(signal.signalId)}
                      />
                      <FieldLabel
                        className="time-waveform-workspace__signal-label"
                        htmlFor={signal.signalId}
                      >
                        {signal.displayName}
                      </FieldLabel>
                    </Field>
                  )
                })}
              </div>
            </div>
          )}
        </section>
      ))}
    </div>

    <footer className="time-waveform-workspace__selection-footprint" aria-label="Selection summary">
      <span className="time-waveform-workspace__selection-footprint-label">
        {selectedSignalIds.length} selected
      </span>
    </footer>
  </aside>
)

export { RecordingRail }
