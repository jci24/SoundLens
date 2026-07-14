import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ComparePairBuilder } from './ComparePairBuilder'
import type { ITimeWaveformRecording } from '../../types'

const recordings: ITimeWaveformRecording[] = [
  {
    recordingId: 'recording-a',
    fileName: 'alpha.wav',
    sizeBytes: 1_024,
    durationSeconds: 1.25,
    sampleRate: 44_100,
    channels: 2,
    channelMode: 'Stereo',
    signals: [],
  },
  {
    recordingId: 'recording-b',
    fileName: 'beta.wav',
    sizeBytes: 2_048,
    durationSeconds: 0.8,
    sampleRate: 48_000,
    channels: 1,
    channelMode: 'Mono',
    signals: [],
  },
  {
    recordingId: 'recording-c',
    fileName: 'gamma.wav',
    sizeBytes: 4_096,
    durationSeconds: 12.4,
    sampleRate: 48_000,
    channels: 4,
    channelMode: 'Multichannel',
    signals: [],
  },
]

describe('ComparePairBuilder', () => {
  it('shows a clear empty state before recordings are available', () => {
    render(
      <ComparePairBuilder
        onRecordingGroupAssignment={vi.fn()}
        onSwap={vi.fn()}
        recordings={[]}
        recordingGroupAssignments={{}}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Choose Compare A recording' }))
    expect(screen.getByText('Import a recording to create a comparison pair.')).toBeInTheDocument()
  })

  it('chooses an empty target from an anchored picker with compact metadata', () => {
    const onRecordingGroupAssignment = vi.fn()

    render(
      <ComparePairBuilder
        onRecordingGroupAssignment={onRecordingGroupAssignment}
        onSwap={vi.fn()}
        recordings={recordings}
        recordingGroupAssignments={{}}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Choose Compare A recording' }))

    expect(screen.getByRole('dialog', { name: 'Select Compare A recording' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Choose Compare A recording' })).toHaveAttribute(
      'aria-expanded',
      'true'
    )
    expect(screen.getByRole('button', { name: /alpha\.wav.*1\.25 s.*2 channels/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /gamma\.wav.*12\.4 s.*4 channels/i })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /beta\.wav.*0\.80 s.*1 channel/i }))

    expect(onRecordingGroupAssignment).toHaveBeenCalledWith('recording-b', 'A')
    expect(screen.queryByLabelText('Select Compare A recording')).not.toBeInTheDocument()
  })

  it('supports replacing and clearing a populated target', () => {
    const onRecordingGroupAssignment = vi.fn()

    render(
      <ComparePairBuilder
        onRecordingGroupAssignment={onRecordingGroupAssignment}
        onSwap={vi.fn()}
        recordings={recordings}
        recordingGroupAssignments={{ 'recording-a': 'A' }}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Replace Compare A recording' }))
    fireEvent.click(screen.getByRole('button', { name: /gamma\.wav.*12\.4 s.*4 channels/i }))
    fireEvent.click(screen.getByRole('button', { name: 'Clear Compare A recording' }))

    expect(onRecordingGroupAssignment).toHaveBeenNthCalledWith(1, 'recording-c', 'A')
    expect(onRecordingGroupAssignment).toHaveBeenNthCalledWith(2, 'recording-a', 'unassigned')
  })

  it('disables the opposite target recording and exposes an atomic swap action', () => {
    const onSwap = vi.fn()

    render(
      <ComparePairBuilder
        onRecordingGroupAssignment={vi.fn()}
        onSwap={onSwap}
        recordings={recordings}
        recordingGroupAssignments={{ 'recording-a': 'A', 'recording-b': 'B' }}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Replace Compare A recording' }))

    expect(screen.getByRole('button', { name: /beta\.wav.*In use/i })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'Swap A/B' }))
    expect(onSwap).toHaveBeenCalledOnce()
  })

  it('closes on Escape and returns focus to the trigger', async () => {
    render(
      <ComparePairBuilder
        onRecordingGroupAssignment={vi.fn()}
        onSwap={vi.fn()}
        recordings={recordings}
        recordingGroupAssignments={{}}
      />
    )

    const trigger = screen.getByRole('button', { name: 'Choose Compare B recording' })
    trigger.focus()
    fireEvent.click(trigger)
    fireEvent.keyDown(document, { key: 'Escape' })

    await waitFor(() => {
      expect(screen.queryByLabelText('Select Compare B recording')).not.toBeInTheDocument()
      expect(trigger).toHaveFocus()
    })
  })

  it('requires an explicit resolution when a target has inconsistent assignments', () => {
    render(
      <ComparePairBuilder
        onRecordingGroupAssignment={vi.fn()}
        onSwap={vi.fn()}
        recordings={recordings}
        recordingGroupAssignments={{ 'recording-a': 'A', 'recording-b': 'A' }}
      />
    )

    expect(screen.getByRole('button', { name: 'Replace Compare A recording' })).toHaveTextContent(
      '2 recordings assigned'
    )
    expect(screen.queryByRole('button', { name: 'Swap A/B' })).not.toBeInTheDocument()
  })
})
