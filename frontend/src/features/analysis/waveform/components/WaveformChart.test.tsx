import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { useRef } from 'react'
import { describe, expect, it } from 'vitest'
import { RecordingPlaybackProvider } from '../../playback/components/RecordingPlaybackProvider'
import { useRecordingPlaybackContext } from '../../playback/contexts/recordingPlaybackContext'
import type { ITimeWaveformAxis, ITimeWaveformRecording, ITimeWaveformSignal } from '../../types'
import { WaveformChart } from './WaveformChart'

const recording: ITimeWaveformRecording = {
  recordingId: 'recording-1',
  fileName: 'recording.wav',
  sizeBytes: 1_024,
  durationSeconds: 1,
  sampleRate: 44_100,
  channels: 1,
  channelMode: 'Mono',
  signals: [],
}

const signal: ITimeWaveformSignal = {
  signalId: 'signal-1',
  recordingId: recording.recordingId,
  recordingFileName: recording.fileName,
  displayName: 'Channel 1',
  durationSeconds: 1,
  sampleRate: 44_100,
  channelIndex: 0,
  amplitudeUnit: 'FS',
  isCalibrated: false,
  findings: [],
  bins: [[-0.5, 0.5], [-0.25, 0.25]],
}

const yAxis: ITimeWaveformAxis = {
  unit: 'FS',
  minimum: -1,
  maximum: 1,
  ticks: [-1, 0, 1],
}

const PlaybackChartHarness = ({ recordingId = recording.recordingId }: { recordingId?: string }) => {
  const playback = useRecordingPlaybackContext()

  return (
    <>
      <button type="button" onClick={() => playback.selectRecording(recording.recordingId)}>
        Select source
      </button>
      <button type="button" onClick={() => playback.seek(0.5)}>
        Move playhead
      </button>
      <WaveformChart
        signals={[{ ...signal, recordingId }]}
        width={600}
        yAxis={yAxis}
      />
    </>
  )
}

const renderPlaybackChart = (recordingId?: string) => {
  const TestWorkspace = () => {
    const workspaceRef = useRef<HTMLElement | null>(null)

    return (
      <section ref={workspaceRef}>
        <RecordingPlaybackProvider
          recordings={[recording]}
          recordingGroupAssignments={{}}
          regionOfInterest={null}
          workspaceRef={workspaceRef}
        >
          <PlaybackChartHarness recordingId={recordingId} />
        </RecordingPlaybackProvider>
      </section>
    )
  }

  return render(<TestWorkspace />)
}

describe('WaveformChart playback position', () => {
  it('renders a non-interactive playhead for a chart containing the selected recording', async () => {
    const { container } = renderPlaybackChart()

    expect(container.querySelector('.time-waveform-workspace__playhead')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Select source' }))

    await waitFor(() => {
      expect(container.querySelector('.time-waveform-workspace__playhead')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByRole('button', { name: 'Move playhead' }))

    await waitFor(() => {
      expect(container.querySelector('.time-waveform-workspace__playhead-line')).toHaveAttribute('x1', '317')
    })
  })

  it('does not show the playhead on split charts for another recording', async () => {
    const { container } = renderPlaybackChart('recording-2')

    fireEvent.click(screen.getByRole('button', { name: 'Select source' }))

    await waitFor(() => {
      expect(container.querySelector('.time-waveform-workspace__playhead')).not.toBeInTheDocument()
    })
  })
})
