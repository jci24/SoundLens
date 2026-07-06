import { ChevronRight } from 'lucide-react'
import { Checkbox } from '@/components/ui/checkbox'
import { Field, FieldLabel } from '@/components/ui/field'
import type { ITimeWaveformRecording } from '../../types'
import './RecordingRail.scss'

interface IRecordingRailProps {
  expandedRecordings: string[]
  onRecordingToggle: (recordingId: string) => void
  onSignalSelection: (signalId: string) => void
  recordings: ITimeWaveformRecording[]
  selectedSignalIds: string[]
}

const RecordingRail = ({
  expandedRecordings,
  onRecordingToggle,
  onSignalSelection,
  recordings,
  selectedSignalIds,
}: IRecordingRailProps) => (
  <aside className="time-waveform-workspace__recording-rail" aria-label="Imported recordings and channels">
    <div className="time-waveform-workspace__recording-list">
      {recordings.map((recording) => (
        <section className="time-waveform-workspace__recording-group" key={recording.recordingId}>
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

          {expandedRecordings.includes(recording.recordingId) && (
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
