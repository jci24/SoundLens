import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import { CopilotPanel } from './CopilotPanel'

const submitSpy = vi.fn()

vi.mock('../hooks/useCopilotQuery', () => ({
  useCopilotQuery: () => ({
    turns: [],
    isLoading: false,
    submit: submitSpy,
    retry: vi.fn(),
  }),
}))

vi.mock('./CopilotInput', () => ({
  CopilotInput: ({ onSubmit }: { onSubmit: (question: string) => void }) => (
    <button type="button" onClick={() => onSubmit('Explain this difference')}>
      Ask
    </button>
  ),
}))

describe('CopilotPanel', () => {
  beforeEach(() => {
    submitSpy.mockReset()
    useAnalysisWorkspaceStore.setState({
      comparisonCopilotContext: {
        recordingIdA: 'recording-a',
        recordingFileNameA: 'alpha.wav',
        recordingIdB: 'recording-b',
        recordingFileNameB: 'beta.wav',
        metricKey: 'crestFactorDelta',
        metricLabel: 'Crest factor',
        unit: 'ratio',
        comparedPairCount: 2,
        missingValueCount: 0,
        meanDifference: -0.075,
        medianDifference: -0.075,
        spread: 0.284,
        coverageLabel: 'Stronger evidence',
        coverageCopy: 'The selected metric is supported by the currently aligned evidence set.',
        limitations: [],
        observation: {
          signalIdA: 'signal-a',
          displayNameA: 'Channel 1',
          signalIdB: 'signal-b',
          displayNameB: 'Channel 1',
          valueA: 5.062,
          valueB: 5.279,
          delta: -0.217,
        },
        findings: [
          {
            signalId: 'signal-a',
            label: 'Dominant tonal component',
            detail: 'Peak around 257 Hz.',
          },
        ],
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
      signalIds: ['signal-a', 'signal-b'],
      startTimeSeconds: 0.25,
      endTimeSeconds: 0.75,
      comparisonContext: expect.objectContaining({
        metricKey: 'crestFactorDelta',
        recordingFileNameA: 'alpha.wav',
        recordingFileNameB: 'beta.wav',
      }),
    })
  })
})
