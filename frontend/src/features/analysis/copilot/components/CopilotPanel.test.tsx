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
    })
  })
})
