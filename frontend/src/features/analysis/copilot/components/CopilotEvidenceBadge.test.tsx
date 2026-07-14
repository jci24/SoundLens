import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import { CopilotEvidenceBadge } from './CopilotEvidenceBadge'

describe('CopilotEvidenceBadge', () => {
  it('shows the recording filename together with the signal display name', () => {
    useAnalysisWorkspaceStore.setState({
      recordings: [
        {
          recordingId: 'recording-1',
          fileName: 'motor-a.wav',
          sizeBytes: 1024,
          durationSeconds: 1,
          sampleRate: 44_100,
          channels: 2,
          channelMode: 'Stereo',
          signals: [
            {
              signalId: 'signal-1',
              channelIndex: 0,
              displayName: 'Channel 1',
            },
          ],
        },
      ],
    })

    render(
      <CopilotEvidenceBadge
        item={{
          toolName: 'compare_signals',
          signalId: 'signal-1',
          summary: 'RMS amplitude: -16.3 dBFS',
        }}
      />
    )

    expect(screen.getByText('Compare')).toBeInTheDocument()
    expect(screen.getByText('motor-a.wav · Channel 1')).toBeInTheDocument()
  })

  it('shows the active comparison pair for selected comparison context evidence', () => {
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
        findings: [],
      },
      recordings: [],
    })

    render(
      <CopilotEvidenceBadge
        item={{
          toolName: 'selected_comparison_context',
          signalId: '',
          summary: 'Crest factor · alpha.wav vs beta.wav',
        }}
      />
    )

    expect(screen.getByText('Compare view')).toBeInTheDocument()
    expect(screen.getByText('alpha.wav vs beta.wav')).toBeInTheDocument()
  })
})
