import { useDeferredValue, useRef, useState } from 'react'
import { observeElementRect, useVirtualizer } from '@tanstack/react-virtual'
import { ChevronRight, Search, X } from 'lucide-react'
import { Checkbox } from '@/components/ui/checkbox'
import { Field, FieldLabel } from '@/components/ui/field'
import type { ITimeWaveformRecording, TComparisonGroupAssignment } from '../../types'
import {
  buildRecordingRailRows,
  filterRecordingRailRecordings,
} from '../utils/recordingRailRows'
import { ComparePairBuilder } from './ComparePairBuilder'
import './RecordingRail.scss'

const recordingSearchThreshold = 8
const recordingRowHeight = 42
const signalRowHeight = 36
const recordingListFallbackRect = { height: 320, width: 240 }

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
  const [searchQuery, setSearchQuery] = useState('')
  const deferredSearchQuery = useDeferredValue(searchQuery)
  const listRef = useRef<HTMLDivElement | null>(null)
  const filteredRecordings = filterRecordingRailRecordings(recordings, deferredSearchQuery)
  const rows = buildRecordingRailRows(filteredRecordings, expandedRecordings)
  const showSearch = recordings.length > recordingSearchThreshold
  // TanStack Virtual owns imperative measurements, so this hook is intentionally outside React Compiler memoization.
  // eslint-disable-next-line react-hooks/incompatible-library
  const virtualizer = useVirtualizer({
    count: rows.length,
    estimateSize: (index) => rows[index]?.kind === 'signal' ? signalRowHeight : recordingRowHeight,
    getItemKey: (index) => rows[index]?.key ?? index,
    getScrollElement: () => listRef.current,
    initialRect: recordingListFallbackRect,
    measureElement: (element) => {
      const measuredHeight = element.getBoundingClientRect().height
      const rowIndex = Number(element.getAttribute('data-index'))

      if (measuredHeight > 0) {
        return measuredHeight
      }

      return rows[rowIndex]?.kind === 'signal' ? signalRowHeight : recordingRowHeight
    },
    observeElementRect: (instance, callback) => observeElementRect(instance, (rect) => callback({
      height: rect.height || recordingListFallbackRect.height,
      width: rect.width || recordingListFallbackRect.width,
    })),
    overscan: 5,
  })

  const handleSearchChange = (query: string) => {
    setSearchQuery(query)
    virtualizer.scrollToOffset(0)
  }

  return (
    <aside className="time-waveform-workspace__recording-rail" aria-label="Imported recordings and channels">
      <ComparePairBuilder
        onRecordingGroupAssignment={onRecordingGroupAssignment}
        onSwap={onComparisonTargetsSwap}
        recordings={recordings}
        recordingGroupAssignments={recordingGroupAssignments}
      />

      <div className="time-waveform-workspace__recording-rail-header">
        <div className="time-waveform-workspace__recording-rail-heading">
          <span className="time-waveform-workspace__recording-rail-title">Recordings</span>
          <span className="time-waveform-workspace__recording-rail-summary">
            {filteredRecordings.length}{showSearch && filteredRecordings.length !== recordings.length ? ` of ${recordings.length}` : ''}
            {selectedSignalIds.length > 0 ? ` · ${selectedSignalIds.length} selected` : ''}
          </span>
        </div>
        {showSearch && (
          <label className="time-waveform-workspace__recording-search">
            <Search aria-hidden="true" size={13} />
            <input
              aria-label="Filter recordings"
              onChange={(event) => handleSearchChange(event.target.value)}
              placeholder="Filter recordings"
              type="search"
              value={searchQuery}
            />
            {searchQuery && (
              <button
                aria-label="Clear recording filter"
                type="button"
                onClick={() => handleSearchChange('')}
              >
                <X aria-hidden="true" size={12} />
              </button>
            )}
          </label>
        )}
      </div>

      <div className="time-waveform-workspace__recording-list" ref={listRef} role="list">
        {rows.length === 0 && (
          <p className="time-waveform-workspace__recording-empty">No recordings match this filter.</p>
        )}
        <div
          className="time-waveform-workspace__recording-virtualizer"
          style={{ height: virtualizer.getTotalSize() }}
        >
          {virtualizer.getVirtualItems().map((virtualRow) => {
            const row = rows[virtualRow.index]

            if (!row) {
              return null
            }

            return (
              <div
                className={`time-waveform-workspace__recording-virtual-row time-waveform-workspace__recording-virtual-row--${row.kind}`}
                data-index={virtualRow.index}
                key={row.key}
                ref={virtualizer.measureElement}
                style={{ transform: `translateY(${virtualRow.start}px)` }}
              >
                {row.kind === 'recording' ? (
                  <div
                    className="time-waveform-workspace__recording-group-header"
                    role="listitem"
                  >
                    <button
                      aria-expanded={expandedRecordings.includes(row.recording.recordingId)}
                      className="time-waveform-workspace__recording-toggle"
                      type="button"
                      onClick={() => onRecordingToggle(row.recording.recordingId)}
                    >
                      <span className="time-waveform-workspace__recording-heading">
                        <ChevronRight
                          className={`time-waveform-workspace__recording-chevron${expandedRecordings.includes(row.recording.recordingId) ? ' time-waveform-workspace__recording-chevron--open' : ''}`}
                          size={14}
                        />
                        <span className="time-waveform-workspace__recording-name">{row.recording.fileName}</span>
                      </span>
                    </button>
                    {recordingGroupAssignments[row.recording.recordingId] !== undefined &&
                      recordingGroupAssignments[row.recording.recordingId] !== 'unassigned' && (
                        <span
                          aria-label={`Compare ${recordingGroupAssignments[row.recording.recordingId]}`}
                          className="time-waveform-workspace__recording-assignment-badge"
                        >
                          {recordingGroupAssignments[row.recording.recordingId]}
                        </span>
                      )}
                  </div>
                ) : (
                  <Field
                    className={`time-waveform-workspace__signal-row${selectedSignalIds.includes(row.signal.signalId) ? ' time-waveform-workspace__signal-row--selected' : ''}`}
                    data-selected={selectedSignalIds.includes(row.signal.signalId)}
                    orientation="horizontal"
                    role="listitem"
                  >
                    <Checkbox
                      checked={selectedSignalIds.includes(row.signal.signalId)}
                      className="time-waveform-workspace__signal-checkbox"
                      id={row.signal.signalId}
                      onCheckedChange={() => onSignalSelection(row.signal.signalId)}
                    />
                    <FieldLabel
                      className="time-waveform-workspace__signal-label"
                      htmlFor={row.signal.signalId}
                    >
                      {row.signal.displayName}
                    </FieldLabel>
                  </Field>
                )}
              </div>
            )
          })}
        </div>
      </div>
    </aside>
  )
}

export { RecordingRail }
