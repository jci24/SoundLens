import { ChevronRight } from 'lucide-react'
import { Checkbox } from '@/components/ui/checkbox'
import { Field, FieldLabel } from '@/components/ui/field'
import type { ITimeWaveformRecording, TComparisonGroupAssignment } from '../../types'
import { ComparePairBuilder } from './ComparePairBuilder'
import './RecordingRail.scss'

interface IRecordingRailProps {
  expandedRecordings: string[]
  onComparisonTargetsSwap: () => void
  onRecordingGroupAssignment: (recordingId: string, assignment: TComparisonGroupAssignment) => void
  onRecordingToggle: (recordingId: string) => void
  onSignalSelection: (signalId: string) => void
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  selectedSignalIds: string[]
}

const RecordingRail = ({
  expandedRecordings,
  onComparisonTargetsSwap,
  onRecordingGroupAssignment,
  onRecordingToggle,
  onSignalSelection,
  recordings,
  recordingGroupAssignments,
  selectedSignalIds,
}: IRecordingRailProps) => {
  return (
    <aside className="time-waveform-workspace__recording-rail" aria-label="Imported recordings and channels">
      <ComparePairBuilder
        onRecordingGroupAssignment={onRecordingGroupAssignment}
        onSwap={onComparisonTargetsSwap}
        recordings={recordings}
        recordingGroupAssignments={recordingGroupAssignments}
      />

      <div className="time-waveform-workspace__recording-rail-header">
        <span className="time-waveform-workspace__recording-rail-title">Recordings</span>
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
              {recordingGroupAssignments[recording.recordingId] !== undefined &&
                recordingGroupAssignments[recording.recordingId] !== 'unassigned' && (
                  <span
                    aria-label={`Compare ${recordingGroupAssignments[recording.recordingId]}`}
                    className="time-waveform-workspace__recording-assignment-badge"
                  >
                    {recordingGroupAssignments[recording.recordingId]}
                  </span>
                )}
            </div>

            {expandedRecordings.includes(recording.recordingId) && (
              <div className="time-waveform-workspace__recording-detail">
                <div className="time-waveform-workspace__signal-list">
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
}

export { RecordingRail }
