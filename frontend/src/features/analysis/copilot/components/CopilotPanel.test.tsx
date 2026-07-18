import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import type { ITimeWaveformRecording } from '../../types'
import type { TCopilotContextMode } from '../types/copilot.types'
import { CopilotPanel } from './CopilotPanel'

const submitSpy = vi.fn()
let submittedQuestion = 'Explain this difference'
let submittedContextMode: TCopilotContextMode = 'auto'
const recordings: ITimeWaveformRecording[] = [
  {
    recordingId: 'recording-a',
    fileName: 'baseline.wav',
    sizeBytes: 1024,
    durationSeconds: 2.5,
    sampleRate: 44100,
    channels: 1,
    channelMode: 'mono',
    signals: [{ signalId: 'signal-a', channelIndex: 0, displayName: 'Channel 1' }],
  },
  {
    recordingId: 'recording-b',
    fileName: 'candidate.wav',
    sizeBytes: 1024,
    durationSeconds: 2.5,
    sampleRate: 44100,
    channels: 1,
    channelMode: 'mono',
    signals: [{ signalId: 'signal-b', channelIndex: 0, displayName: 'Channel 1' }],
  },
]

vi.mock('../hooks/useCopilotQuery', () => ({
  useCopilotQuery: () => ({
    turns: [],
    isLoading: false,
    submit: submitSpy,
    retry: vi.fn(),
  }),
}))

vi.mock('./CopilotInput', () => ({
  CopilotInput: ({ onSubmit }: { onSubmit: (question: string, contextMode: TCopilotContextMode) => void }) => (
    <button type="button" onClick={() => onSubmit(submittedQuestion, submittedContextMode)}>
      Ask
    </button>
  ),
}))

describe('CopilotPanel', () => {
  beforeEach(() => {
    submitSpy.mockReset()
    submittedQuestion = 'Explain this difference'
    submittedContextMode = 'auto'
    useAnalysisWorkspaceStore.setState({
      recordingGroupAssignments: {},
      comparisonCopilotContext: {
        recordingIdA: 'recording-a',
        recordingIdB: 'recording-b',
        metricKey: 'crestFactorDelta',
        signalIdA: 'signal-a',
        signalIdB: 'signal-b',
      },
    })
  })

  it('submits the active comparison context together with the current ROI', () => {
    render(
      <CopilotPanel
        recordings={[]}
        regionOfInterest={{
          startTimeSeconds: 0.25,
          endTimeSeconds: 0.75,
          durationSeconds: 0.5,
        }}
        selectedSignalIds={['signal-a', 'signal-b']}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))

    expect(submitSpy).toHaveBeenCalledWith({
      question: 'Explain this difference',
      contextMode: 'auto',
      signalIds: ['signal-a', 'signal-b'],
      startTimeSeconds: 0.25,
      endTimeSeconds: 0.75,
      comparisonContext: {
        metricKey: 'crestFactorDelta',
        recordingIdA: 'recording-a',
        recordingIdB: 'recording-b',
        signalIdA: 'signal-a',
        signalIdB: 'signal-b',
      },
      comparisonPair: undefined,
    })
  })

  it('uses the aligned comparison pair even when the general selection contains one signal', () => {
    render(
      <CopilotPanel
        recordings={[]}
        regionOfInterest={null}
        selectedSignalIds={['signal-a']}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))

    expect(submitSpy).toHaveBeenCalledWith(expect.objectContaining({
      signalIds: ['signal-a', 'signal-b'],
      comparisonContext: expect.objectContaining({
        signalIdA: 'signal-a',
        signalIdB: 'signal-b',
      }),
    }))
  })

  it('uses the visible focused signal when no comparison or mention overrides it', () => {
    useAnalysisWorkspaceStore.setState({ comparisonCopilotContext: null })
    submittedQuestion = 'What is the RMS level?'
    render(
      <CopilotPanel
        recordings={[]}
        regionOfInterest={null}
        selectedSignalIds={['signal-focused']}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))

    expect(submitSpy).toHaveBeenCalledWith({
      question: 'What is the RMS level?',
      contextMode: 'auto',
      signalIds: ['signal-focused'],
      startTimeSeconds: undefined,
      endTimeSeconds: undefined,
      comparisonContext: undefined,
      comparisonPair: undefined,
    })
  })

  it('keeps the focused signal and includes an assigned A/B pair for comparison questions', () => {
    useAnalysisWorkspaceStore.setState({
      comparisonCopilotContext: null,
      recordingGroupAssignments: { 'recording-a': 'A', 'recording-b': 'B' },
    })
    submittedQuestion = 'Which signal is louder by RMS?'
    render(
      <CopilotPanel
        recordings={recordings}
        regionOfInterest={null}
        selectedSignalIds={['signal-a']}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))

    expect(submitSpy).toHaveBeenCalledWith({
      question: 'Which signal is louder by RMS?',
      contextMode: 'auto',
      signalIds: ['signal-a'],
      startTimeSeconds: undefined,
      endTimeSeconds: undefined,
      comparisonContext: undefined,
      comparisonPair: {
        recordingIdA: 'recording-a',
        recordingIdB: 'recording-b',
      },
    })
  })

  it('lets explicit mentions override the selected comparison scope', () => {
    submittedQuestion = 'Inspect @[Reference](signal-reference) RMS'
    render(
      <CopilotPanel
        recordings={[]}
        regionOfInterest={null}
        selectedSignalIds={['signal-a']}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))

    expect(submitSpy).toHaveBeenCalledWith({
      question: 'Inspect @Reference RMS',
      contextMode: 'workspace',
      signalIds: ['signal-reference'],
      startTimeSeconds: undefined,
      endTimeSeconds: undefined,
      comparisonContext: undefined,
      comparisonPair: undefined,
    })
  })

  it('omits every workspace identifier when General mode is forced', () => {
    submittedQuestion = 'Explain the Nyquist theorem.'
    submittedContextMode = 'general'
    render(
      <CopilotPanel
        recordings={recordings}
        regionOfInterest={{
          startTimeSeconds: 0.25,
          endTimeSeconds: 0.75,
          durationSeconds: 0.5,
        }}
        selectedSignalIds={['signal-a', 'signal-b']}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))

    expect(submitSpy).toHaveBeenCalledWith({
      question: 'Explain the Nyquist theorem.',
      contextMode: 'general',
      signalIds: undefined,
      startTimeSeconds: undefined,
      endTimeSeconds: undefined,
      comparisonContext: undefined,
      comparisonPair: undefined,
    })
  })
})
