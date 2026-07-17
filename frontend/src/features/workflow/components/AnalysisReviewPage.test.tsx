import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAnalysisWorkspaceStore } from '../../analysis/stores/useAnalysisWorkspaceStore'
import { AnalysisReviewPage } from './AnalysisReviewPage'

vi.mock('../../import/hooks/useImportedRecordingInventory', () => ({
  useImportedRecordingInventory: () => ({
    error: null,
    inventory: {
      recordings: [
        {
          recordingId: 'recording-a', fileName: 'baseline.wav', sizeBytes: 128,
          durationSeconds: 2, sampleRate: 48_000, channels: 1, channelMode: 'mono', signals: [],
        },
        {
          recordingId: 'recording-b', fileName: 'candidate.wav', sizeBytes: 256,
          durationSeconds: 2.5, sampleRate: 44_100, channels: 2, channelMode: 'stereo', signals: [],
        },
      ],
      failedFiles: [],
    },
    retry: vi.fn(),
    status: 'ready',
  }),
}))

const renderPage = () => render(
  <MemoryRouter initialEntries={['/analysis']}>
    <Routes>
      <Route path="analysis" element={<AnalysisReviewPage />} />
      <Route path="evidence" element={<div>Evidence destination</div>} />
      <Route path="setup" element={<div>Configuration destination</div>} />
    </Routes>
  </MemoryRouter>
)

describe('AnalysisReviewPage', () => {
  beforeEach(() => {
    useAnalysisWorkspaceStore.setState({
      activeSurface: 'waveform',
      enabledAnalysisSurfaces: ['waveform', 'spectrum'],
      layoutMode: 'focused',
      recordingGroupAssignments: {},
      regionOfInterest: null,
    })
  })

  it('selects only shipped analyses and keeps at least one enabled', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: 'Select analyses' })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Include Time waveform' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Include Frequency spectrum' })).toBeChecked()
    expect(screen.queryByText(/spectrogram|CPB|batch/i)).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Time waveform' }))
    expect(useAnalysisWorkspaceStore.getState().enabledAnalysisSurfaces).toEqual(['spectrum'])
    expect(useAnalysisWorkspaceStore.getState().activeSurface).toBe('spectrum')

    fireEvent.click(screen.getByRole('checkbox', { name: 'Include Frequency spectrum' }))
    expect(useAnalysisWorkspaceStore.getState().enabledAnalysisSurfaces).toEqual(['spectrum'])
  })

  it('reviews a valid comparison and opens Evidence without fabricating execution progress', () => {
    useAnalysisWorkspaceStore.setState({
      layoutMode: 'compare',
      recordingGroupAssignments: {
        'recording-a': 'A',
        'recording-b': 'B',
      },
    })

    renderPage()

    expect(screen.getByText('baseline.wav vs candidate.wav')).toBeInTheDocument()
    expect(screen.getByText('Comparison metrics')).toBeInTheDocument()
    expect(screen.getByText(/Peak, RMS, crest factor, and clipping/i)).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Run selected analyses' }))
    expect(screen.getByText('Evidence destination')).toBeInTheDocument()
  })

  it('blocks comparison execution when the pair is incomplete', () => {
    useAnalysisWorkspaceStore.setState({
      layoutMode: 'compare',
      recordingGroupAssignments: { 'recording-a': 'A' },
    })

    renderPage()

    expect(screen.getByText('Comparison pair incomplete')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Run selected analyses' })).toBeDisabled()
    expect(screen.getByRole('alert')).toHaveTextContent(/Choose one Compare A and one Compare B/i)
  })
})
