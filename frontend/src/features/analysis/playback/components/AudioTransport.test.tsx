import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { useRef } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { IAnalysisRegionOfInterest, ITimeWaveformRecording } from '../../types'
import { AudioTransport } from './AudioTransport'
import { RecordingPlaybackProvider } from './RecordingPlaybackProvider'

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

interface IPlaybackTestWorkspaceProps {
  layoutMode?: 'focused' | 'compare'
  recordings?: ITimeWaveformRecording[]
  regionOfInterest?: IAnalysisRegionOfInterest | null
  recordingGroupAssignments?: Record<string, 'unassigned' | 'A' | 'B'>
}

const PlaybackTestWorkspace = ({
  layoutMode = 'focused',
  recordings = [createRecording(1)],
  regionOfInterest = null,
  recordingGroupAssignments = {},
}: IPlaybackTestWorkspaceProps) => {
  const workspaceRef = useRef<HTMLElement | null>(null)

  return (
    <>
      <section ref={workspaceRef}>
        <RecordingPlaybackProvider
          layoutMode={layoutMode}
          recordings={recordings}
          recordingGroupAssignments={recordingGroupAssignments}
          regionOfInterest={regionOfInterest}
          workspaceRef={workspaceRef}
        >
          <AudioTransport />
        </RecordingPlaybackProvider>
      </section>
      <textarea aria-label="Copilot composer" />
    </>
  )
}

const renderTransport = ({
  layoutMode = 'focused',
  recordings = [createRecording(1)],
  regionOfInterest = null,
  recordingGroupAssignments = {},
}: IPlaybackTestWorkspaceProps = {}) => render(
  <PlaybackTestWorkspace
    layoutMode={layoutMode}
    recordings={recordings}
    recordingGroupAssignments={recordingGroupAssignments}
    regionOfInterest={regionOfInterest}
  />
)

describe('AudioTransport', () => {
  const play = vi.fn<() => Promise<void>>()
  const pause = vi.fn()
  const load = vi.fn()
  let animationFrameCallback: FrameRequestCallback | null = null

  beforeEach(() => {
    play.mockReset()
    pause.mockReset()
    load.mockReset()
    play.mockResolvedValue(undefined)
    vi.spyOn(HTMLMediaElement.prototype, 'play').mockImplementation(play)
    vi.spyOn(HTMLMediaElement.prototype, 'pause').mockImplementation(pause)
    vi.spyOn(HTMLMediaElement.prototype, 'load').mockImplementation(load)
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation((callback) => {
      animationFrameCallback = callback
      return 1
    })
    vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => {
      animationFrameCallback = null
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('keeps one unloaded media element until a recording is explicitly selected', () => {
    const { container } = renderTransport()

    expect(container.querySelectorAll('audio')).toHaveLength(1)
    expect(container.querySelector('audio')).not.toHaveAttribute('src')
    expect(screen.getByRole('button', { name: 'Play recording' })).toBeDisabled()
  })

  it('offers position-aligned A/B audition only for one valid compare pair', async () => {
    const recordings = [createRecording(1), createRecording(2), createRecording(3)]
    const { container } = renderTransport({
      layoutMode: 'compare',
      recordings,
      recordingGroupAssignments: {
        'recording-1': 'A',
        'recording-2': 'B',
      },
    })

    const auditionA = screen.getByRole('button', { name: /Audition Compare A: recording-001\.wav/i })
    const auditionB = screen.getByRole('button', { name: /Audition Compare B: recording-002\.wav/i })
    expect(auditionA).toHaveAttribute('aria-pressed', 'false')
    expect(container.querySelectorAll('audio')).toHaveLength(1)

    fireEvent.click(auditionA)
    await waitFor(() => expect(auditionA).toHaveAttribute('aria-pressed', 'true'))
    expect(container.querySelectorAll('audio')).toHaveLength(2)
    expect(container.querySelectorAll('audio')[0]).toHaveAttribute(
      'src',
      'http://127.0.0.1:5123/api/playback/recordings/recording-1'
    )
    expect(container.querySelectorAll('audio')[1]).toHaveAttribute(
      'src',
      'http://127.0.0.1:5123/api/playback/recordings/recording-2'
    )

    fireEvent.click(auditionB)
    await waitFor(() => expect(auditionB).toHaveAttribute('aria-pressed', 'true'))
    expect(container.querySelectorAll('audio')).toHaveLength(2)
  })

  it('does not expose A/B audition outside compare mode or for an invalid pair', () => {
    const recordings = [createRecording(1), createRecording(2)]
    const { rerender } = renderTransport({
      recordings,
      recordingGroupAssignments: { 'recording-1': 'A', 'recording-2': 'B' },
    })

    expect(screen.queryByRole('button', { name: /Audition Compare A/i })).not.toBeInTheDocument()

    rerender(
      <PlaybackTestWorkspace
        layoutMode="compare"
        recordings={recordings}
        recordingGroupAssignments={{ 'recording-1': 'A' }}
      />
    )
    expect(screen.queryByRole('button', { name: /Audition Compare A/i })).not.toBeInTheDocument()
  })

  it('releases the standby source when the active pair becomes invalid', async () => {
    const recordings = [createRecording(1), createRecording(2)]
    const { container, rerender } = renderTransport({
      layoutMode: 'compare',
      recordings,
      recordingGroupAssignments: { 'recording-1': 'A', 'recording-2': 'B' },
    })

    fireEvent.click(screen.getByRole('button', { name: /Audition Compare A/i }))
    await waitFor(() => expect(container.querySelectorAll('audio')).toHaveLength(2))

    rerender(
      <PlaybackTestWorkspace
        layoutMode="compare"
        recordings={recordings}
        recordingGroupAssignments={{ 'recording-1': 'A' }}
      />
    )

    await waitFor(() => expect(container.querySelectorAll('audio')).toHaveLength(1))
    expect(screen.queryByRole('button', { name: /Audition Compare A/i })).not.toBeInTheDocument()
    expect(pause).toHaveBeenCalled()
    expect(load).toHaveBeenCalled()
  })

  it('keeps the logical position and resumes only after the switched source is ready', async () => {
    const { container } = renderTransport({
      layoutMode: 'compare',
      recordings: [createRecording(1), createRecording(2)],
      recordingGroupAssignments: { 'recording-1': 'A', 'recording-2': 'B' },
    })

    fireEvent.click(screen.getByRole('button', { name: /Audition Compare A/i }))
    const audio = container.querySelector('audio')!
    fireEvent.loadedMetadata(audio)
    fireEvent.change(screen.getByRole('slider', { name: 'Playback position' }), {
      target: { value: '24.5' },
    })
    Object.defineProperty(audio, 'paused', { configurable: true, value: false })
    fireEvent.click(screen.getByRole('button', { name: /Audition Compare B/i }))

    expect(screen.getByText('Loading B')).toBeInTheDocument()
    expect(play).not.toHaveBeenCalled()
    fireEvent.loadedMetadata(audio)

    await waitFor(() => expect(play).toHaveBeenCalledOnce())
    expect(audio.currentTime).toBe(24.5)
  })

  it('clamps an A/B switch to the target recording ROI boundary', async () => {
    const shortTarget = { ...createRecording(2), durationSeconds: 12 }
    const { container } = renderTransport({
      layoutMode: 'compare',
      recordings: [createRecording(1), shortTarget],
      recordingGroupAssignments: { 'recording-1': 'A', 'recording-2': 'B' },
      regionOfInterest: { startTimeSeconds: 5, endTimeSeconds: 20, durationSeconds: 15 },
    })

    fireEvent.click(screen.getByRole('button', { name: /Audition Compare A/i }))
    const audio = container.querySelector('audio')!
    fireEvent.loadedMetadata(audio)
    fireEvent.change(screen.getByRole('slider', { name: 'Playback position' }), {
      target: { value: '18' },
    })
    fireEvent.click(screen.getByRole('button', { name: /Audition Compare B/i }))
    fireEvent.loadedMetadata(audio)

    await waitFor(() => expect(audio.currentTime).toBe(12))
    expect(screen.getByText('0:12 / 0:12')).toBeInTheDocument()
  })

  it('selects one recording from a searchable bounded picker with metadata', async () => {
    const recordings = Array.from({ length: 100 }, (_, index) => createRecording(index + 1))
    const { container } = renderTransport({
      recordings,
      recordingGroupAssignments: { 'recording-100': 'B' },
    })

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
    renderTransport()
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
    const { container } = renderTransport()

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    const audio = container.querySelector('audio')!

    fireEvent.loadedMetadata(audio)
    await waitFor(() => expect(screen.getByRole('button', { name: 'Play recording' })).toBeEnabled())
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
    const { container, unmount } = renderTransport()

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
    renderTransport()

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    fireEvent.loadedMetadata(document.querySelector('audio')!)
    await waitFor(() => expect(screen.getByRole('button', { name: 'Play recording' })).toBeEnabled())

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Play recording' }))
    })

    expect(screen.getByText('Playback could not start.')).toBeInTheDocument()
  })

  it('starts within the ROI, stops at its end, and resets when the ROI changes', async () => {
    const firstRegion = {
      startTimeSeconds: 10,
      endTimeSeconds: 20,
      durationSeconds: 10,
    }
    const { container, rerender } = renderTransport({ regionOfInterest: firstRegion })

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    const audio = container.querySelector('audio')!
    fireEvent.loadedMetadata(audio)
    await waitFor(() => expect(screen.getByRole('button', { name: 'Play recording' })).toBeEnabled())

    expect(audio.currentTime).toBe(10)
    expect(screen.getByRole('slider', { name: 'Playback position' })).toHaveAttribute('min', '10')
    expect(screen.getByRole('slider', { name: 'Playback position' })).toHaveAttribute('max', '20')

    fireEvent.click(screen.getByRole('button', { name: 'Play recording' }))
    await waitFor(() => expect(play).toHaveBeenCalledOnce())
    fireEvent.playing(audio)
    audio.currentTime = 20

    act(() => animationFrameCallback?.(16))

    expect(pause).toHaveBeenCalled()
    expect(audio.currentTime).toBe(20)

    rerender(
      <PlaybackTestWorkspace
        recordings={[createRecording(1)]}
        recordingGroupAssignments={{}}
        regionOfInterest={{ startTimeSeconds: 30, endTimeSeconds: 40, durationSeconds: 10 }}
      />
    )

    await waitFor(() => expect(audio.currentTime).toBe(30))
    expect(screen.getByText('0:30 / 0:40')).toBeInTheDocument()
  })

  it('loops the ROI only after the user explicitly enables it', async () => {
    const { container } = renderTransport({
      regionOfInterest: {
        startTimeSeconds: 5,
        endTimeSeconds: 8,
        durationSeconds: 3,
      },
    })

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    const audio = container.querySelector('audio')!
    fireEvent.loadedMetadata(audio)
    await waitFor(() => expect(screen.getByRole('button', { name: 'Play recording' })).toBeEnabled())
    const loopButton = screen.getByRole('button', { name: 'Loop selected region' })

    expect(loopButton).toHaveAttribute('aria-pressed', 'false')
    fireEvent.click(loopButton)
    expect(loopButton).toHaveAttribute('aria-pressed', 'true')
    fireEvent.click(screen.getByRole('button', { name: 'Play recording' }))
    await waitFor(() => expect(play).toHaveBeenCalledOnce())
    fireEvent.playing(audio)
    audio.currentTime = 8

    act(() => animationFrameCallback?.(16))

    expect(audio.currentTime).toBe(5)
    expect(screen.getByText('0:05 / 0:08')).toBeInTheDocument()
  })

  it('stops and resets to the ROI start when the playback source is replaced', async () => {
    const { container } = renderTransport({
      recordings: [createRecording(1), createRecording(2)],
      regionOfInterest: {
        startTimeSeconds: 5,
        endTimeSeconds: 8,
        durationSeconds: 3,
      },
    })

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    const audio = container.querySelector('audio')!
    fireEvent.loadedMetadata(audio)
    fireEvent.change(screen.getByRole('slider', { name: 'Playback position' }), {
      target: { value: '7' },
    })
    expect(audio.currentTime).toBe(7)

    fireEvent.click(screen.getByRole('button', { name: 'Change playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-002\.wav/i }))

    await waitFor(() => {
      expect(audio).toHaveAttribute(
        'src',
        'http://127.0.0.1:5123/api/playback/recordings/recording-2'
      )
      expect(audio.currentTime).toBe(5)
    })
    expect(pause).toHaveBeenCalled()
  })

  it('guards Spacebar playback from interactive and editable controls', async () => {
    const { container } = renderTransport()

    fireEvent.click(screen.getByRole('button', { name: 'Choose playback recording' }))
    fireEvent.click(screen.getByRole('button', { name: /recording-001\.wav/i }))
    fireEvent.loadedMetadata(container.querySelector('audio')!)
    await waitFor(() => expect(screen.getByRole('button', { name: 'Play recording' })).toBeEnabled())

    const slider = screen.getByRole('slider', { name: 'Playback position' })
    slider.focus()
    fireEvent.keyDown(document, {
      code: 'Space',
    })
    expect(play).not.toHaveBeenCalled()

    const composer = screen.getByRole('textbox', { name: 'Copilot composer' })
    composer.focus()
    fireEvent.keyDown(document, { code: 'Space' })
    expect(play).not.toHaveBeenCalled()

    composer.blur()
    await act(async () => {
      fireEvent.keyDown(document, { code: 'Space' })
    })
    await waitFor(() => expect(play).toHaveBeenCalledOnce())
  })
})
