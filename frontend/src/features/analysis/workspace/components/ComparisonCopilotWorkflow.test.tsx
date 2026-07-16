import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { CopilotPanel } from '../../copilot/components/CopilotPanel'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import { TimeWaveformWorkspace } from './TimeWaveformWorkspace'
import { comparisonResponse, groundedResponse, importedFiles, recordings } from './ComparisonCopilotWorkflow.fixtures'
import type { IAnalysisRegionOfInterest } from '../../types'

const mockGetRecordingComparison = vi.fn()
const mockPostAgentQuery = vi.fn()
const mockUseTimeWaveformWorkspace = vi.fn()

vi.mock('../../services/recordingComparison', () => ({
  getRecordingComparison: (...args: unknown[]) => mockGetRecordingComparison(...args),
}))

vi.mock('../../copilot/services/copilotService', () => ({
  postAgentQuery: (...args: unknown[]) => mockPostAgentQuery(...args),
}))

vi.mock('../hooks/useTimeWaveformWorkspace', () => ({
  useTimeWaveformWorkspace: (...args: unknown[]) => mockUseTimeWaveformWorkspace(...args),
}))

vi.mock('../hooks/useAnalysisWorkspacePanels', () => ({
  useAnalysisWorkspacePanels: () => ({ hasActiveChart: true, panels: [] }),
}))

vi.mock('../../metrics/hooks/useAnalysisWorkspaceMetrics', () => ({
  useAnalysisWorkspaceMetrics: () => ({ hasMetricsPending: false, metricSignals: [] }),
}))

vi.mock('../../report/hooks/useReportExport', () => ({
  useReportExport: () => ({
    canExportReport: false,
    comparisonReportFormat: 'markdown',
    comparisonReportTitle: '',
    excludedRecordings: [],
    handleComparisonReportExport: vi.fn(),
    handleExportReport: vi.fn(),
    isComparisonReportOpen: false,
    isExporting: false,
    setComparisonReportTitle: vi.fn(),
    setComparisonReportFormat: vi.fn(),
    setIsComparisonReportOpen: vi.fn(),
  }),
}))

vi.mock('../../recording-rail/components/RecordingRail', () => ({ RecordingRail: () => null }))
vi.mock('./AnalysisWorkspaceHeader', () => ({ AnalysisWorkspaceHeader: () => null }))
vi.mock('./ComparisonEvidenceInspector', () => ({ ComparisonEvidenceInspector: () => null }))
vi.mock('../../report/components/ComparisonReportDialog', () => ({ ComparisonReportDialog: () => null }))
vi.mock('../../playback/components/AudioTransport', () => ({ AudioTransport: () => null }))
vi.mock('../../playback/components/RecordingPlaybackProvider', () => ({
  RecordingPlaybackProvider: ({ children }: { children: React.ReactNode }) => children,
}))
vi.mock('./AnalysisWorkspaceChart', () => ({
  AnalysisWorkspaceChart: ({
    onRegionOfInterestChange,
  }: {
    onRegionOfInterestChange: (region: IAnalysisRegionOfInterest | null) => void
  }) => (
    <button
      type="button"
      onClick={() => onRegionOfInterestChange({
        startTimeSeconds: 0.2,
        endTimeSeconds: 0.8,
        durationSeconds: 0.6,
      })}
    >
      Select workflow region
    </button>
  ),
}))

function useWorkflowWorkspaceState() {
  const state = useAnalysisWorkspaceStore()

  return {
    activeSurface: state.activeSurface,
    chartRef: { current: null },
    chartWidth: 720,
    expandedRecordings: state.expandedRecordings,
    isSpectrumInitialLoading: false,
    isSpectrumRefreshing: false,
    isWaveformInitialLoading: false,
    isWaveformRefreshing: false,
    layoutMode: state.layoutMode,
    recordings: state.recordings,
    recordingGroupAssignments: state.recordingGroupAssignments,
    regionOfInterest: state.regionOfInterest,
    selectedSignalIds: state.selectedSignalIds,
    selectedSpectrumPreset: '4096',
    showSpectrumPanel: false,
    showWaveformPanel: true,
    signalChartMode: state.signalChartMode,
    spectrum: null,
    spectrumError: null,
    spectrumFftSizeOptions: ['1024', '4096'],
    spectrumMaximumHz: 22050,
    spectrumRangeEndHz: 22050,
    spectrumRangeStartHz: 0,
    spectrumSignals: [],
    spectrumViewport: null,
    spectrumXAxis: null,
    waveformError: null,
    waveformSignals: [],
    waveforms: { yAxis: { unit: 'FS', minimum: -1, maximum: 1, ticks: [-1, 0, 1] } },
    onComparisonTargetsSwap: state.swapComparisonTargets,
    onLayoutModeChange: state.setLayoutMode,
    onRecordingGroupAssignment: state.setRecordingGroupAssignment,
    onRecordingToggle: state.toggleRecording,
    onRegionOfInterestChange: state.setRegionOfInterest,
    onSignalChartModeChange: state.setSignalChartMode,
    onSignalSelection: state.selectSignal,
    onSpectrumPresetChange: vi.fn(),
    onSpectrumRangeEndChange: vi.fn(),
    onSpectrumRangeReset: vi.fn(),
    onSpectrumRangeStartChange: vi.fn(),
    onSurfaceChange: state.setActiveSurface,
  }
}

const CopilotHarness = () => {
  const selectedSignalIds = useAnalysisWorkspaceStore((state) => state.selectedSignalIds)
  const regionOfInterest = useAnalysisWorkspaceStore((state) => state.regionOfInterest)
  const workspaceRecordings = useAnalysisWorkspaceStore((state) => state.recordings)

  return (
    <>
      <TimeWaveformWorkspace importedFiles={importedFiles} isCopilotOpen onCopilotToggle={vi.fn()} />
      <CopilotPanel
        recordings={workspaceRecordings}
        regionOfInterest={regionOfInterest}
        selectedSignalIds={selectedSignalIds}
      />
    </>
  )
}

const resetWorkspaceStore = () => {
  useAnalysisWorkspaceStore.setState({
    selectedSignalIds: ['signal-a', 'signal-b'],
    expandedRecordings: [],
    recordingGroupAssignments: { 'recording-a': 'A', 'recording-b': 'B' },
    activeSurface: 'waveform',
    layoutMode: 'compare',
    signalChartMode: 'overlay',
    regionOfInterest: { startTimeSeconds: 0.1, endTimeSeconds: 0.4, durationSeconds: 0.3 },
    recordings,
    comparisonCopilotContext: null,
  })
}

const submitQuestion = (question: string) => {
  fireEvent.change(screen.getByLabelText('Investigation question'), { target: { value: question } })
  fireEvent.click(screen.getByRole('button', { name: 'Investigate' }))
}

describe('comparison-to-Copilot workflow', () => {
  beforeEach(() => {
    mockGetRecordingComparison.mockReset()
    mockPostAgentQuery.mockReset()
    mockUseTimeWaveformWorkspace.mockReset()
    mockUseTimeWaveformWorkspace.mockImplementation(useWorkflowWorkspaceState)
    mockGetRecordingComparison.mockResolvedValue(comparisonResponse)
    resetWorkspaceStore()
  })

  afterEach(() => {
    cleanup()
    expect(useAnalysisWorkspaceStore.getState().comparisonCopilotContext).toBeNull()
    resetWorkspaceStore()
  })

  it('submits identifier-only context and refreshes the metric and ROI without reordering cards', async () => {
    mockPostAgentQuery.mockResolvedValue(groundedResponse)
    const view = render(<CopilotHarness />)

    await waitFor(() => expect(useAnalysisWorkspaceStore.getState().comparisonCopilotContext).toEqual({
      recordingIdA: 'recording-a',
      recordingIdB: 'recording-b',
      metricKey: 'peakAmplitudeDelta',
      signalIdA: 'signal-a',
      signalIdB: 'signal-b',
    }))

    submitQuestion('Explain the selected comparison')

    await waitFor(() => expect(mockPostAgentQuery).toHaveBeenCalledTimes(1))
    expect(mockPostAgentQuery.mock.calls[0]?.[0]).toEqual({
      question: 'Explain the selected comparison',
      signalIds: ['signal-a', 'signal-b'],
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
      comparisonContext: {
        recordingIdA: 'recording-a',
        recordingIdB: 'recording-b',
        metricKey: 'peakAmplitudeDelta',
        signalIdA: 'signal-a',
        signalIdB: 'signal-b',
      },
    })
    expect(Object.keys(mockPostAgentQuery.mock.calls[0]?.[0]?.comparisonContext).sort()).toEqual([
      'metricKey', 'recordingIdA', 'recordingIdB', 'signalIdA', 'signalIdB',
    ])

    const metricCardsBefore = Array.from(
      view.container.querySelectorAll('.time-waveform-workspace__comparison-metric-label')
    ).map((item) => item.textContent)
    fireEvent.click(screen.getByRole('button', { name: /RMS amplitude/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Select workflow region' }))

    await waitFor(() => expect(useAnalysisWorkspaceStore.getState().comparisonCopilotContext?.metricKey)
      .toBe('rmsAmplitudeDelta'))
    await waitFor(() => expect(useAnalysisWorkspaceStore.getState().regionOfInterest).toMatchObject({
      startTimeSeconds: 0.2,
      endTimeSeconds: 0.8,
    }))
    expect(useAnalysisWorkspaceStore.getState().regionOfInterest?.durationSeconds).toBeCloseTo(0.6)

    submitQuestion('Explain the updated selection')
    await waitFor(() => expect(mockPostAgentQuery).toHaveBeenCalledTimes(2))
    expect(mockPostAgentQuery.mock.calls[1]?.[0]).toMatchObject({
      startTimeSeconds: 0.2,
      endTimeSeconds: 0.8,
      comparisonContext: { metricKey: 'rmsAmplitudeDelta' },
    })
    expect(Array.from(
      view.container.querySelectorAll('.time-waveform-workspace__comparison-metric-label')
    ).map((item) => item.textContent)).toEqual(metricCardsBefore)
  })

  it('renders loading and a deterministic refusal through the normal grounded response contract', async () => {
    let resolveResponse: (value: typeof groundedResponse) => void = () => undefined
    mockPostAgentQuery.mockImplementation(() => new Promise((resolve) => { resolveResponse = resolve }))
    render(<CopilotHarness />)

    await waitFor(() => expect(useAnalysisWorkspaceStore.getState().comparisonCopilotContext).not.toBeNull())
    submitQuestion('What is the calibrated dB SPL difference?')

    expect(await screen.findByRole('status')).toHaveTextContent('Thinking')
    expect(screen.getByLabelText('Investigation question')).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Investigate' })).toBeDisabled()

    resolveResponse({
      ...groundedResponse,
      answer: 'Calibrated physical SPL cannot be determined from the selected digital evidence.',
      limitations: ['Amplitude values are not calibrated to physical SPL.'],
      nextSteps: ['Provide a validated acoustic calibration reference.'],
    })

    expect(await screen.findByText(/Calibrated physical SPL cannot be determined/)).toBeInTheDocument()
    expect(screen.getByLabelText('Evidence used')).toHaveTextContent('Compare')
    expect(screen.getByLabelText('Evidence used')).toHaveTextContent('baseline.wav · Channel 1')
    expect(screen.getByLabelText('Limitations')).toHaveTextContent('not calibrated to physical SPL')
    expect(screen.getByLabelText('Suggested next steps')).toHaveTextContent('validated acoustic calibration')
  })

  it('recovers after failure and re-runs a completed turn with its original context', async () => {
    mockPostAgentQuery
      .mockRejectedValueOnce(new Error('Comparison explanation failed'))
      .mockResolvedValueOnce(groundedResponse)
      .mockResolvedValueOnce({ ...groundedResponse, answer: 'Re-run answer' })
    render(<CopilotHarness />)

    await waitFor(() => expect(useAnalysisWorkspaceStore.getState().comparisonCopilotContext).not.toBeNull())
    submitQuestion('First attempt')
    expect(await screen.findByRole('alert')).toHaveTextContent('Comparison explanation failed')

    fireEvent.click(screen.getByRole('button', { name: /RMS amplitude/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Select workflow region' }))
    await waitFor(() => expect(useAnalysisWorkspaceStore.getState().comparisonCopilotContext?.metricKey)
      .toBe('rmsAmplitudeDelta'))

    submitQuestion('Recovered attempt')
    expect(await screen.findByText(groundedResponse.answer)).toBeInTheDocument()
    const recoveredRequest = mockPostAgentQuery.mock.calls[1]?.[0]

    fireEvent.click(screen.getByRole('button', { name: /Crest factor/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Clear selected comparison region' }))
    await waitFor(() => expect(useAnalysisWorkspaceStore.getState().comparisonCopilotContext?.metricKey)
      .toBe('crestFactorDelta'))
    expect(useAnalysisWorkspaceStore.getState().regionOfInterest).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Re-run' }))
    await waitFor(() => expect(mockPostAgentQuery).toHaveBeenCalledTimes(3))
    expect(mockPostAgentQuery.mock.calls[2]?.[0]).toEqual(recoveredRequest)
    expect(await screen.findByText('Re-run answer')).toBeInTheDocument()
  })
})
