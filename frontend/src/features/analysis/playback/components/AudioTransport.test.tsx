import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ITimeWaveformRecording } from '../../types'
import { AudioTransport } from './AudioTransport'

const createRecording = (index: number): ITimeWaveformRecording => ({
  recordingId: `recording-${index}`,
  fileName: `recording-${String(index).padStart(3, '0')}.wav`,
  sizeBytes: 1_024 + index,
  durationSeconds: 60 + index,
  sampleRate: 44_100,
  channels: index % 2 === 0 ? 2 : 1,
  channelMode: index % 2 === 0 ? 'Stereo' : 'Mono',
  signals: [],
})

describe('AudioTransport', () => {
  const play = vi.fn<() => Promise<void>>()
  const pause = vi.fn()
  const load = vi.fn()

  beforeEach(() => {
    play.mockResolvedValue(undefined)
    vi.spyOn(HTMLMediaElement.prototype, 'play').mockImplementation(play)
    vi.spyOn(HTMLMediaElement.prototype, 'pause').mockImplementation(pause)
    vi.spyOn(HTMLMediaElement.prototype, 'load').mockImplementation(load)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('keeps one unloaded media element until a recording is explicitly selected', () => {
    const { container } = render(
      <AudioTransport recordings={[createRecording(1)]} recordingGroupAssignments={{}} />
    )

    expect(container.querySelectorAll('audio')).toHaveLength(1)
    expect(container.querySelector('audio')).not.toHaveAttribute('src')
    expect(screen.getByRole('button', { name: 'Play recording' })).toBeDisabled()
  })

  it('selects one recording from a searchable bounded picker with metadata', async () => {
    const recordings = Array.from({ length: 100 }, (_, index) => createRecording(index + 1))
    const { container } = render(
      <AudioTransport
        recordings={recordings}
        recordingGroupAssignments={{ 'recording-100': 'B' }}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    expect(screen.getByText('Showing the first 50. Refine the search to see more.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /recording-100\.wav/i })).not.toBeInTheDocument()

    fireEvent.change(screen.getByRole('searchbox', { name: 'Search recordings' }), {
      target: { value: 'recording-100' },
    })

    const finalRecording = await screen.findByRole('button', {
      name: /recording-100\.wav.*2 channels.*Compare B/i,
    })
    fireEvent.click(finalRecording)

    expect(container.querySelectorAll('audio')).toHaveLength(1)
    expect(container.querySelector('audio')).toHaveAttribute(
      'src',
      'http://127.0.0.1:5123/api/playback/recordings/recording-100'
    )
    expect(screen.getByRole('button', { name: 'Change playback recording' })).toHaveTextContent(
      'recording-100.wav'
    )
  })

  it('closes the source picker on Escape and returns focus to its trigger', async () => {
    render(<AudioTransport recordings={[createRecording(1)]} recordingGroupAssignments={{}} />)
    const trigger = screen.getByRole('button', { name: 'Choose playback recording' })

    trigger.focus()
    fireEvent.click(trigger)
    expect(screen.getByRole('searchbox', { name: 'Search recordings' })).toHaveFocus()
    fireEvent.keyDown(document, { key: 'Escape' })

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Select playback recording' })).not.toBeInTheDocument()
      expect(trigger).toHaveFocus()
    })
  })

  it('plays, pauses, seeks, reports media state, and clears the source', async () => {
    const { container } = render(
      <AudioTransport recordings={[createRecording(1)]} recordingGroupAssignments={{}} />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    const audio = container.querySelector('audio')!

    fireEvent.loadedMetadata(audio)
    fireEvent.click(screen.getByRole('button', { name: 'Play recording' }))
    await waitFor(() => expect(play).toHaveBeenCalledOnce())

    fireEvent.playing(audio)
    expect(screen.getByRole('button', { name: 'Pause recording' })).toBeInTheDocument()

    fireEvent.change(screen.getByRole('slider', { name: 'Playback position' }), {
      target: { value: '12.5' },
    })
    expect(audio.currentTime).toBe(12.5)
    expect(screen.getByText('0:12 / 1:01')).toBeInTheDocument()

    fireEvent.waiting(audio)
    expect(screen.getByText('Buffering')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Clear playback recording' }))
    expect(pause).toHaveBeenCalled()
    expect(audio).not.toHaveAttribute('src')
  })

  it('shows an honest unsupported-format error and releases media on unmount', () => {
    const { container, unmount } = render(
      <AudioTransport recordings={[createRecording(1)]} recordingGroupAssignments={{}} />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    const audio = container.querySelector('audio')!
    Object.defineProperty(audio, 'error', {
      configurable: true,
      value: { code: 4 },
    })

    fireEvent.error(audio)
    expect(screen.getByText('This recording format is not supported by the browser.')).toBeInTheDocument()

    unmount()
    expect(pause).toHaveBeenCalled()
    expect(load).toHaveBeenCalled()
  })

  it('reports a failed play request without exposing browser details', async () => {
    play.mockRejectedValueOnce(new DOMException('Autoplay denied'))
    render(<AudioTransport recordings={[createRecording(1)]} recordingGroupAssignments={{}} />)

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    fireEvent.loadedMetadata(document.querySelector('audio')!)

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Play recording' }))
    })

    expect(screen.getByText('Playback could not start.')).toBeInTheDocument()
  })
})
