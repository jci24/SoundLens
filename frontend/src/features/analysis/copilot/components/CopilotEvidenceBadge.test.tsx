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
})
