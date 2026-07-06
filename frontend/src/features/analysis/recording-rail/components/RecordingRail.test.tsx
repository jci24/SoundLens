import { fireEvent, render, screen } from '@testing-library/react'
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
    const onRecordingToggle = vi.fn()
    const onSignalSelection = vi.fn()

    render(
      <RecordingRail
        expandedRecordings={['recording-1']}
        onRecordingToggle={onRecordingToggle}
        onSignalSelection={onSignalSelection}
        recordings={recordings}
        selectedSignalIds={['signal-left']}
      />
    )

    expect(screen.getByText('alpha.wav')).toBeInTheDocument()
    expect(screen.getByText('beta.wav')).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Left' })).toHaveAttribute('data-state', 'checked')
    expect(screen.getByRole('checkbox', { name: 'Right' })).toHaveAttribute('data-state', 'unchecked')
    expect(screen.queryByRole('checkbox', { name: 'Mono' })).not.toBeInTheDocument()
    expect(screen.getByText('1 selected')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /beta\.wav/i }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Right' }))

    expect(onRecordingToggle).toHaveBeenCalledWith('recording-2')
    expect(onSignalSelection).toHaveBeenCalledWith('signal-right')
  })
})
