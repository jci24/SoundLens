import { useDeferredValue, useRef, useState } from 'react'
import * as Popover from '@radix-ui/react-popover'
import { Check, ChevronDown, Search } from 'lucide-react'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import type { ITimeWaveformRecording, TComparisonGroupAssignment } from '../../types'
import './PlaybackRecordingPicker.scss'

interface IPlaybackRecordingPickerProps {
  onSelect: (recordingId: string) => void
  recordings: ITimeWaveformRecording[]
  recordingGroupAssignments: Record<string, TComparisonGroupAssignment>
  selectedRecording: ITimeWaveformRecording | null
}

const maximumVisibleRecordings = 50

const formatRecordingMetadata = (
  recording: ITimeWaveformRecording,
  assignment: TComparisonGroupAssignment | undefined
) => {
  const channelLabel = `${recording.channels} ${recording.channels === 1 ? 'channel' : 'channels'}`
  const assignmentLabel = assignment === 'A' || assignment === 'B' ? ` · Compare ${assignment}` : ''
  return `${formatCompactDuration(recording.durationSeconds)} · ${channelLabel}${assignmentLabel}`
}

const PlaybackRecordingPicker = ({
  onSelect,
  recordings,
  recordingGroupAssignments,
  selectedRecording,
}: IPlaybackRecordingPickerProps) => {
  const searchInputRef = useRef<HTMLInputElement | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const deferredSearchQuery = useDeferredValue(searchQuery)
  const normalizedQuery = deferredSearchQuery.trim().toLocaleLowerCase()
  const matchingRecordings = recordings.filter((recording) => {
    if (!normalizedQuery) {
      return true
    }

    const assignment = recordingGroupAssignments[recording.recordingId]
    const searchableText = [
      recording.fileName,
      recording.channelMode,
      `${recording.channels} channels`,
      assignment === 'A' || assignment === 'B' ? `compare ${assignment}` : '',
    ].join(' ').toLocaleLowerCase()

    return searchableText.includes(normalizedQuery)
  })
  const visibleRecordings = matchingRecordings.slice(0, maximumVisibleRecordings)

  const handleSelect = (recordingId: string) => {
    onSelect(recordingId)
    setSearchQuery('')
    setIsOpen(false)
  }

  return (
    <Popover.Root open={isOpen} onOpenChange={setIsOpen}>
      <Popover.Trigger asChild>
        <button
          aria-label={selectedRecording ? 'Change playback recording' : 'Choose playback recording'}
          className="playback-recording-picker__trigger"
          type="button"
        >
          <span className="playback-recording-picker__trigger-copy">
            <strong>{selectedRecording?.fileName ?? 'Choose recording'}</strong>
            {selectedRecording && (
              <span>
                {formatRecordingMetadata(
                  selectedRecording,
                  recordingGroupAssignments[selectedRecording.recordingId]
                )}
              </span>
            )}
          </span>
          <ChevronDown aria-hidden="true" size={14} />
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          aria-label="Select playback recording"
          className="playback-recording-picker__popover"
          collisionPadding={12}
          onOpenAutoFocus={(event) => {
            event.preventDefault()
            searchInputRef.current?.focus()
          }}
          role="dialog"
          sideOffset={7}
        >
          <label className="playback-recording-picker__search">
            <Search aria-hidden="true" size={14} />
            <span className="sr-only">Search recordings</span>
            <input
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search recordings"
              ref={searchInputRef}
              type="search"
              value={searchQuery}
            />
          </label>

          <div className="playback-recording-picker__result-summary" aria-live="polite">
            {matchingRecordings.length} {matchingRecordings.length === 1 ? 'recording' : 'recordings'}
          </div>

          <div className="playback-recording-picker__options">
            {visibleRecordings.map((recording) => {
              const isSelected = recording.recordingId === selectedRecording?.recordingId

              return (
                <button
                  aria-pressed={isSelected}
                  className="playback-recording-picker__option"
                  key={recording.recordingId}
                  type="button"
                  onClick={() => handleSelect(recording.recordingId)}
                >
                  <span className="playback-recording-picker__option-copy">
                    <strong>{recording.fileName}</strong>
                    <span>
                      {formatRecordingMetadata(
                        recording,
                        recordingGroupAssignments[recording.recordingId]
                      )}
                    </span>
                  </span>
                  {isSelected && <Check aria-hidden="true" size={14} />}
                </button>
              )
            })}

            {matchingRecordings.length === 0 && (
              <p className="playback-recording-picker__empty">No recordings match this search.</p>
            )}
          </div>

          {matchingRecordings.length > maximumVisibleRecordings && (
            <p className="playback-recording-picker__refine">
              Showing the first {maximumVisibleRecordings}. Refine the search to see more.
            </p>
          )}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}

export { PlaybackRecordingPicker }
