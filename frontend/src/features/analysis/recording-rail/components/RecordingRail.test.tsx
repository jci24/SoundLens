import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { RecordingRail } from './RecordingRail'
import type { ITimeWaveformRecording } from '../../types'

const recordings: ITimeWaveformRecording[] = [
  {
    recordingId: 'recording-1',
    fileName: 'alpha.wav',
    sizeBytes: 1_024,
    durationSeconds: 1.25,
    sampleRate: 44_100,
    channels: 2,
    channelMode: 'Stereo',
    signals: [
      {
        signalId: 'signal-left',
        channelIndex: 0,
        displayName: 'Left',
      },
      {
        signalId: 'signal-right',
        channelIndex: 1,
        displayName: 'Right',
      },
    ],
  },
  {
    recordingId: 'recording-2',
    fileName: 'beta.wav',
    sizeBytes: 2_048,
    durationSeconds: 0.8,
    sampleRate: 48_000,
    channels: 1,
    channelMode: 'Mono',
    signals: [
      {
        signalId: 'signal-mono',
        channelIndex: 0,
        displayName: 'Mono',
      },
    ],
  },
]

describe('RecordingRail', () => {
  it('renders expanded signals and dispatches recording and signal actions', () => {
    const onRecordingGroupAssignment = vi.fn()
    const onComparisonTargetsSwap = vi.fn()
    const onRecordingToggle = vi.fn()
    const onSignalSelection = vi.fn()

    render(
      <RecordingRail
        expandedRecordings={['recording-1']}
        onComparisonTargetsSwap={onComparisonTargetsSwap}
        onRecordingGroupAssignment={onRecordingGroupAssignment}
        onRecordingToggle={onRecordingToggle}
        onSignalSelection={onSignalSelection}
        recordings={recordings}
        recordingGroupAssignments={{
          'recording-1': 'A',
          'recording-2': 'unassigned',
        }}
        selectedSignalIds={['signal-left']}
      />
    )

    expect(screen.getAllByText('alpha.wav')).toHaveLength(2)
    expect(screen.getByRole('button', { name: /beta\.wav/i })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Left' })).toHaveAttribute('data-state', 'checked')
    expect(screen.getByRole('checkbox', { name: 'Right' })).toHaveAttribute('data-state', 'unchecked')
    expect(screen.queryByRole('checkbox', { name: 'Mono' })).not.toBeInTheDocument()
    expect(screen.queryByText('Compare pair')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Replace Compare A recording' })).toHaveTextContent('alpha.wav')
    expect(screen.getByRole('button', { name: 'Choose Compare B recording' })).toBeInTheDocument()
    expect(screen.getByText('2 · 1 selected')).toBeInTheDocument()
    expect(screen.getByLabelText('Compare A')).toBeInTheDocument()
    expect(screen.queryByText('Out')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /beta\.wav/i }))
    fireEvent.click(screen.getByRole('button', { name: 'Choose Compare B recording' }))
    fireEvent.click(screen.getByRole('button', { name: /beta\.wav.*0\.80 s.*1 channel/i }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Right' }))

    expect(onRecordingToggle).toHaveBeenCalledWith('recording-2')
    expect(onRecordingGroupAssignment).toHaveBeenCalledWith('recording-2', 'B')
    expect(onSignalSelection).toHaveBeenCalledWith('signal-right')
  })

  it('virtualizes and filters a 100-recording session without losing stable recording actions', async () => {
    const largeSession = Array.from({ length: 100 }, (_, index): ITimeWaveformRecording => ({
      recordingId: `recording-${index + 1}`,
      fileName: `recording-${String(index + 1).padStart(3, '0')}.wav`,
      sizeBytes: 1_024,
      durationSeconds: 1,
      sampleRate: 44_100,
      channels: 2,
      channelMode: 'Stereo',
      signals: [
        { signalId: `signal-${index + 1}-left`, channelIndex: 0, displayName: 'Left' },
        { signalId: `signal-${index + 1}-right`, channelIndex: 1, displayName: 'Right' },
      ],
    }))
    const onRecordingToggle = vi.fn()

    const { rerender } = render(
      <RecordingRail
        expandedRecordings={[]}
        onComparisonTargetsSwap={vi.fn()}
        onRecordingGroupAssignment={vi.fn()}
        onRecordingToggle={onRecordingToggle}
        onSignalSelection={vi.fn()}
        recordings={largeSession}
        recordingGroupAssignments={{ 'recording-100': 'B' }}
        selectedSignalIds={[]}
      />
    )

    expect(screen.getAllByRole('listitem').length).toBeLessThan(25)
    expect(screen.queryByRole('button', { name: /recording-100\.wav/i })).not.toBeInTheDocument()

    fireEvent.change(screen.getByRole('searchbox', { name: 'Filter recordings' }), {
      target: { value: 'recording-100' },
    })

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /recording-100\.wav/i })).toBeInTheDocument()
    })
    expect(screen.getByText('1 of 100')).toBeInTheDocument()
    expect(screen.getByLabelText('Compare B')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /recording-100\.wav/i }))
    expect(onRecordingToggle).toHaveBeenCalledWith('recording-100')

    rerender(
      <RecordingRail
        expandedRecordings={['recording-100']}
        onComparisonTargetsSwap={vi.fn()}
        onRecordingGroupAssignment={vi.fn()}
        onRecordingToggle={onRecordingToggle}
        onSignalSelection={vi.fn()}
        recordings={largeSession}
        recordingGroupAssignments={{ 'recording-100': 'B' }}
        selectedSignalIds={['signal-100-left']}
      />
    )

    await waitFor(() => {
      expect(screen.getByRole('checkbox', { name: 'Left' })).toHaveAttribute('data-state', 'checked')
    })

    fireEvent.click(screen.getByRole('button', { name: 'Clear recording filter' }))
    expect(screen.queryByRole('checkbox', { name: 'Left' })).not.toBeInTheDocument()

    fireEvent.change(screen.getByRole('searchbox', { name: 'Filter recordings' }), {
      target: { value: 'recording-100' },
    })

    await waitFor(() => {
      expect(screen.getByRole('checkbox', { name: 'Left' })).toHaveAttribute('data-state', 'checked')
    })
  })
})
