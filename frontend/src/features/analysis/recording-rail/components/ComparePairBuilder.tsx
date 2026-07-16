import { useState } from 'react'
import * as Popover from '@radix-ui/react-popover'
import { ArrowUpDown, Check, ChevronDown, Search, X } from 'lucide-react'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import type { ITimeWaveformRecording, TComparisonGroupAssignment } from '../../types'
import './ComparePairBuilder.scss'

interface IComparePairBuilderProps {
  onRecordingGroupAssignment: (recordingId: string, assignment: TComparisonGroupAssignment) => void
  onSwap: () => void
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
}

interface IComparePairSlotProps {
  assignment: 'A' | 'B'
  disabledRecordingIds: string[]
  onRecordingGroupAssignment: (recordingId: string, assignment: TComparisonGroupAssignment) => void
  recordings: ITimeWaveformRecording[]
  selectedRecordings: ITimeWaveformRecording[]
}

const maximumVisibleRecordings = 50

const formatRecordingMetadata = (recording: ITimeWaveformRecording) =>
  `${formatCompactDuration(recording.durationSeconds)} · ${recording.channels} ${recording.channels === 1 ? 'channel' : 'channels'}`

const ComparePairSlot = ({
  assignment,
  disabledRecordingIds,
  onRecordingGroupAssignment,
  recordings,
  selectedRecordings,
}: IComparePairSlotProps) => {
  const [isOpen, setIsOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const selectedRecording = selectedRecordings.length === 1 ? selectedRecordings[0] : null
  const hasConflict = selectedRecordings.length > 1
  const action = selectedRecording || hasConflict ? 'Replace' : 'Choose'
  const normalizedSearchQuery = searchQuery.trim().toLocaleLowerCase()
  const matchingRecordings = normalizedSearchQuery
    ? recordings.filter((recording) => recording.fileName.toLocaleLowerCase().includes(normalizedSearchQuery))
    : recordings
  const visibleRecordings = matchingRecordings.slice(0, maximumVisibleRecordings)

  const handleSelect = (recordingId: string) => {
    onRecordingGroupAssignment(recordingId, assignment)
    setSearchQuery('')
    setIsOpen(false)
  }

  const handleOpenChange = (open: boolean) => {
    setIsOpen(open)

    if (!open) {
      setSearchQuery('')
    }
  }

  return (
    <div className="compare-pair-builder__slot">
      <div className="compare-pair-builder__slot-label-row">
        <span className="compare-pair-builder__slot-label">Compare {assignment}</span>
        {selectedRecording && (
          <button
            aria-label={`Clear Compare ${assignment} recording`}
            className="compare-pair-builder__clear"
            type="button"
            onClick={() => onRecordingGroupAssignment(selectedRecording.recordingId, 'unassigned')}
          >
            <X aria-hidden="true" size={13} />
            <span>Clear</span>
          </button>
        )}
      </div>

      <Popover.Root open={isOpen} onOpenChange={handleOpenChange}>
        <Popover.Trigger asChild>
          <button
            aria-label={`${action} Compare ${assignment} recording`}
            className={`compare-pair-builder__trigger${hasConflict ? ' compare-pair-builder__trigger--conflict' : ''}`}
            type="button"
          >
            <span className="compare-pair-builder__trigger-copy">
              <strong>
                {hasConflict
                  ? `${selectedRecordings.length} recordings assigned`
                  : selectedRecording?.fileName ?? 'Choose recording'}
              </strong>
              {hasConflict && <span>Choose one recording</span>}
              {selectedRecording && <span>{formatRecordingMetadata(selectedRecording)}</span>}
            </span>
            <ChevronDown aria-hidden="true" size={15} />
          </button>
        </Popover.Trigger>

        <Popover.Portal>
          <Popover.Content
            align="start"
            aria-label={`Select Compare ${assignment} recording`}
            className="compare-pair-builder__popover"
            collisionPadding={12}
            role="dialog"
            side="right"
            sideOffset={8}
          >
            <div className="compare-pair-builder__popover-header">
              <span className="compare-pair-builder__popover-title">Select Compare {assignment}</span>
              <span>{matchingRecordings.length}</span>
            </div>
            {recordings.length > 8 && (
              <label className="compare-pair-builder__search">
                <Search aria-hidden="true" size={13} />
                <input
                  aria-label={`Filter Compare ${assignment} recordings`}
                  onChange={(event) => setSearchQuery(event.target.value)}
                  placeholder="Filter recordings"
                  type="search"
                  value={searchQuery}
                />
              </label>
            )}
            <div className="compare-pair-builder__options">
              {recordings.length === 0 && (
                <p className="compare-pair-builder__empty">Import a recording to create a comparison pair.</p>
              )}
              {recordings.length > 0 && matchingRecordings.length === 0 && (
                <p className="compare-pair-builder__empty">No recordings match this filter.</p>
              )}
              {visibleRecordings.map((recording) => {
                const isDisabled = disabledRecordingIds.includes(recording.recordingId)
                const isSelected = selectedRecordings.some(
                  (selected) => selected.recordingId === recording.recordingId
                )

                return (
                  <button
                    aria-pressed={isSelected}
                    className="compare-pair-builder__option"
                    disabled={isDisabled}
                    key={recording.recordingId}
                    type="button"
                    onClick={() => handleSelect(recording.recordingId)}
                  >
                    <span className="compare-pair-builder__option-copy">
                      <strong>{recording.fileName}</strong>
                      <span>{formatRecordingMetadata(recording)}</span>
                    </span>
                    {isSelected && <Check aria-hidden="true" size={14} />}
                    {isDisabled && <span className="compare-pair-builder__option-status">In use</span>}
                  </button>
                )
              })}
            </div>
            {matchingRecordings.length > maximumVisibleRecordings && (
              <p className="compare-pair-builder__result-limit">
                Showing the first {maximumVisibleRecordings}. Refine the filter to see more.
              </p>
            )}
          </Popover.Content>
        </Popover.Portal>
      </Popover.Root>
    </div>
  )
}

const ComparePairBuilder = ({
  onRecordingGroupAssignment,
  onSwap,
  recordings,
  recordingGroupAssignments,
}: IComparePairBuilderProps) => {
  const recordingsA = recordings.filter(
    (recording) => recordingGroupAssignments[recording.recordingId] === 'A'
  )
  const recordingsB = recordings.filter(
    (recording) => recordingGroupAssignments[recording.recordingId] === 'B'
  )
  const canSwap = recordingsA.length === 1 && recordingsB.length === 1

  return (
    <section aria-label="Compare pair builder" className="compare-pair-builder">
      {canSwap && (
        <div className="compare-pair-builder__actions">
          <button className="compare-pair-builder__swap" type="button" onClick={onSwap}>
            <ArrowUpDown aria-hidden="true" size={13} />
            Swap A/B
          </button>
        </div>
      )}

      <ComparePairSlot
        assignment="A"
        disabledRecordingIds={recordingsB.map((recording) => recording.recordingId)}
        onRecordingGroupAssignment={onRecordingGroupAssignment}
        recordings={recordings}
        selectedRecordings={recordingsA}
      />
      <ComparePairSlot
        assignment="B"
        disabledRecordingIds={recordingsA.map((recording) => recording.recordingId)}
        onRecordingGroupAssignment={onRecordingGroupAssignment}
        recordings={recordings}
        selectedRecordings={recordingsB}
      />
    </section>
  )
}

export { ComparePairBuilder }
