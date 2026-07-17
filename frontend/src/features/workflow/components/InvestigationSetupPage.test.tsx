import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAnalysisWorkspaceStore } from '../../analysis/stores/useAnalysisWorkspaceStore'
import { InvestigationSetupPage } from './InvestigationSetupPage'

vi.mock('../../import/hooks/useImportedRecordingInventory', () => ({
  useImportedRecordingInventory: () => ({
    error: null,
    inventory: {
      recordings: [
        {
          recordingId: 'recording-a', fileName: 'baseline.wav', sizeBytes: 128,
          durationSeconds: 2, sampleRate: 48_000, channels: 1, channelMode: 'mono',
          signals: [{ signalId: 'recording-a:ch:0', channelIndex: 0, displayName: 'Channel 1' }],
        },
        {
          recordingId: 'recording-b', fileName: 'candidate.wav', sizeBytes: 256,
          durationSeconds: 2.5, sampleRate: 44_100, channels: 2, channelMode: 'discrete multi-channel',
          signals: [
            { signalId: 'recording-b:ch:0', channelIndex: 0, displayName: 'Channel 1' },
            { signalId: 'recording-b:ch:1', channelIndex: 1, displayName: 'Channel 2' },
          ],
        },
      ],
      failedFiles: [],
    },
    retry: vi.fn(),
    status: 'ready',
  }),
}))

const renderPage = () => render(
  <MemoryRouter initialEntries={['/setup']}>
    <Routes>
      <Route path="setup" element={<InvestigationSetupPage />} />
      <Route path="analysis" element={<div>Analysis destination</div>} />
      <Route path="evidence" element={<div>Evidence destination</div>} />
    </Routes>
  </MemoryRouter>
)

describe('InvestigationSetupPage', () => {
  beforeEach(() => {
    useAnalysisWorkspaceStore.setState({
      layoutMode: 'focused',
      recordingGroupAssignments: {},
      recordings: [],
    })
  })

  it('presents backend metadata and keeps focused evidence available without a pair', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: 'Configure comparison' })).toBeInTheDocument()
    expect(screen.getByText('48.0 kHz')).toBeInTheDocument()
    expect(screen.getByText('Channel 1, Channel 2')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Review comparison analyses' })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'Open focused evidence' }))
    expect(screen.getByText('Evidence destination')).toBeInTheDocument()
    expect(useAnalysisWorkspaceStore.getState().layoutMode).toBe('focused')
  })

  it('reuses atomic A/B assignment and opens comparison evidence when complete', () => {
    renderPage()

    fireEvent.click(screen.getByRole('button', { name: 'Choose Compare A recording' }))
    fireEvent.click(screen.getByRole('button', { name: /baseline\.wav.*1 channel/i }))
    fireEvent.click(screen.getByRole('button', { name: 'Choose Compare B recording' }))
    fireEvent.click(screen.getByRole('button', { name: /candidate\.wav.*2 channels/i }))

    expect(useAnalysisWorkspaceStore.getState().recordingGroupAssignments).toEqual({
      'recording-a': 'A',
      'recording-b': 'B',
    })
    const openComparison = screen.getByRole('button', { name: 'Review comparison analyses' })
    expect(openComparison).toBeEnabled()
    fireEvent.click(openComparison)
    expect(screen.getByText('Analysis destination')).toBeInTheDocument()
    expect(useAnalysisWorkspaceStore.getState().layoutMode).toBe('compare')
  })
})
