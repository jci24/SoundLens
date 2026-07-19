import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TimeWaveformWorkspace } from './TimeWaveformWorkspace'
import type { IMetricSignalItem } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import type {
  IAnalysisRegionOfInterest,
  IFrequencySpectrumSignal,
  IRecordingComparisonResponse,
  TAnalysisLayoutMode,
  TSignalChartMode,
  ITimeWaveformRecording,
} from '../../types'

const mockUseTimeWaveformWorkspace = vi.fn()
const mockUseAnalysisWorkspacePanels = vi.fn()
const mockUseAnalysisWorkspaceMetrics = vi.fn()
const mockExportReportMarkdown = vi.fn()
const mockExportComparisonReportMarkdown = vi.fn()
const mockExportComparisonReportPdf = vi.fn()
const mockDownloadBlobFile = vi.fn()
const mockDownloadTextFile = vi.fn()
const mockGetRecordingComparison = vi.fn()

vi.mock('../hooks/useTimeWaveformWorkspace', () => ({
  useTimeWaveformWorkspace: (...args: unknown[]) => mockUseTimeWaveformWorkspace(...args),
}))

vi.mock('../hooks/useAnalysisWorkspacePanels', () => ({
  useAnalysisWorkspacePanels: (...args: unknown[]) => mockUseAnalysisWorkspacePanels(...args),
}))

vi.mock('../../metrics/hooks/useAnalysisWorkspaceMetrics', () => ({
  useAnalysisWorkspaceMetrics: (...args: unknown[]) => mockUseAnalysisWorkspaceMetrics(...args),
}))

vi.mock('../../report/services/exportReportMarkdown', () => ({
  exportComparisonReportMarkdown: (...args: unknown[]) => mockExportComparisonReportMarkdown(...args),
  exportReportMarkdown: (...args: unknown[]) => mockExportReportMarkdown(...args),
}))

vi.mock('../../report/services/exportComparisonReportPdf', () => ({
  exportComparisonReportPdf: (...args: unknown[]) => mockExportComparisonReportPdf(...args),
}))

vi.mock('../../report/utils/reportDownload', () => ({
  downloadBlobFile: (...args: unknown[]) => mockDownloadBlobFile(...args),
  downloadTextFile: (...args: unknown[]) => mockDownloadTextFile(...args),
}))

vi.mock('../../services/recordingComparison', () => ({
  getRecordingComparison: (...args: unknown[]) => mockGetRecordingComparison(...args),
}))

vi.mock('./AnalysisWorkspaceHeader', () => ({
  AnalysisWorkspaceHeader: ({
    canExportReport,
    canEnterCompareMode,
    onRecordingsOpen,
    onExportReport,
  }: {
    canExportReport: boolean
    canEnterCompareMode: boolean
    onRecordingsOpen: () => void
    onExportReport: () => void
  }) => (
    <div>
      <button data-testid="workspace-header" disabled={!canExportReport} onClick={onExportReport} type="button">
        Export report
      </button>
      <button onClick={onRecordingsOpen} type="button">Open recordings drawer</button>
      <span>{canEnterCompareMode ? 'Compare enabled' : 'Compare disabled'}</span>
    </div>
  ),
}))

vi.mock('../../recording-rail/components/RecordingRail', () => ({
  RecordingRail: ({
    onComparisonTargetsSwap,
    isDrawerOpen,
    onRecordingGroupAssignment,
    recordingGroupAssignments,
  }: {
    onComparisonTargetsSwap: () => void
    isDrawerOpen?: boolean
    onRecordingGroupAssignment: (recordingId: string, assignment: 'A' | 'B' | 'unassigned') => void
    recordingGroupAssignments: Record<string, 'A' | 'B' | 'unassigned'>
  }) => (
    <div data-testid="recording-rail">
      <button onClick={() => onRecordingGroupAssignment('recording-1', 'A')} type="button">
        Assign recording
      </button>
      <button onClick={onComparisonTargetsSwap} type="button">
        Swap pair
      </button>
      <span>{isDrawerOpen ? 'Recording drawer open' : 'Recording drawer closed'}</span>
      <span>{recordingGroupAssignments['recording-1'] ?? 'unassigned'}</span>
    </div>
  ),
}))

vi.mock('./AnalysisWorkspaceChart', () => ({
  AnalysisWorkspaceChart: ({
    onRegionOfInterestChange,
  }: {
    onRegionOfInterestChange: (regionOfInterest: IAnalysisRegionOfInterest) => void
  }) => (
    <div data-testid="workspace-chart">
      <button
        type="button"
        onClick={() => onRegionOfInterestChange({
          startTimeSeconds: 1.5,
          endTimeSeconds: 2.54,
          durationSeconds: 1.04,
        })}
      >
        Select test region
      </button>
    </div>
  ),
}))

const importedFiles: IImportedFileSummary[] = [
  {
    fileName: 'alpha.wav',
    sizeBytes: 1024,
    filePath: '/tmp/alpha.wav',
    contentType: 'audio/wav',
  },
]

const metricSignals: IMetricSignalItem[] = []
const panels: IAnalysisWorkspacePanel[] = []
const comparisonResponse: IRecordingComparisonResponse = {
  recordingA: {
    recordingId: 'recording-1',
    fileName: 'alpha.wav',
    channels: 1,
    durationSeconds: 1,
  },
  recordingB: {
    recordingId: 'recording-2',
    fileName: 'beta.wav',
    channels: 1,
    durationSeconds: 1,
  },
  alignedSignals: [
    {
      signalIdA: 'signal-a',
      displayNameA: 'Channel 1',
      channelIndexA: 0,
      signalIdB: 'signal-b',
      displayNameB: 'Channel 1',
      channelIndexB: 0,
      basis: 'DisplayName',
    },
  ],
  signalObservations: [
    {
      signalIdA: 'signal-a',
      displayNameA: 'Channel 1',
      channelIndexA: 0,
      signalIdB: 'signal-b',
      displayNameB: 'Channel 1',
      channelIndexB: 0,
      basis: 'DisplayName',
      peakAmplitudeA: 0.8,
      peakAmplitudeB: 0.6,
      peakAmplitudeDelta: 0.2,
      rmsAmplitudeA: 0.5,
      rmsAmplitudeB: 0.3,
      rmsAmplitudeDelta: 0.2,
      crestFactorA: 1.6,
      crestFactorB: 2.0,
      crestFactorDelta: -0.4,
      clippingSampleCountA: 0,
      clippingSampleCountB: 4,
      clippingSampleCountDelta: -4,
      hasClippingA: false,
      hasClippingB: true,
    },
  ],
  aggregateMetrics: [
    {
      metricKey: 'peakAmplitudeDelta',
      unit: 'FS',
      comparedPairCount: 1,
      missingValueCount: 1,
      meanDifference: 0.2,
      medianDifference: 0.2,
      minimumDifference: 0.2,
      maximumDifference: 0.2,
      spread: 0,
    },
    {
      metricKey: 'rmsAmplitudeDelta',
      unit: 'FS',
      comparedPairCount: 1,
      missingValueCount: 1,
      meanDifference: 0.1,
      medianDifference: 0.1,
      minimumDifference: 0.1,
      maximumDifference: 0.1,
      spread: 0,
    },
    {
      metricKey: 'crestFactorDelta',
      unit: 'ratio',
      comparedPairCount: 1,
      missingValueCount: 1,
      meanDifference: -0.4,
      medianDifference: -0.4,
      minimumDifference: -0.4,
      maximumDifference: -0.4,
      spread: 0,
    },
    {
      metricKey: 'clippingSampleCountDelta',
      unit: 'samples',
      comparedPairCount: 1,
      missingValueCount: 1,
      meanDifference: -4,
      medianDifference: -4,
      minimumDifference: -4,
      maximumDifference: -4,
      spread: 0,
    },
  ],
  limitations: [
    {
      code: 'LowCoverage',
      detail: 'Only 1 aligned signal pair is available for aggregate comparison.',
    },
  ],
  regionOfInterest: null,
}

const createWorkspaceState = () => ({
  activeSurface: 'waveform' as const,
  chartRef: { current: null },
  chartWidth: 720,
  expandedRecordings: [],
  isSpectrumInitialLoading: false,
  isSpectrumRefreshing: false,
  isWaveformInitialLoading: false,
  isWaveformRefreshing: false,
  layoutMode: 'focused' as TAnalysisLayoutMode,
  recordings: [] as ITimeWaveformRecording[],
  recordingGroupAssignments: {} as Record<string, 'A' | 'B' | 'unassigned'>,
  selectedSignalIds: [] as string[],
  selectedSpectrumPreset: '4096',
  signalChartMode: 'overlay' as TSignalChartMode,
  showSpectrumPanel: false,
  showWaveformPanel: true,
  spectrum: null,
  spectrumError: null,
  spectrumFftSizeOptions: ['1024', '4096'],
  spectrumMaximumHz: 22050,
  spectrumRangeEndHz: 22050,
  spectrumRangeStartHz: 0,
  spectrumSignals: [] as IFrequencySpectrumSignal[],
  spectrumViewport: null,
  spectrumXAxis: null,
  waveformError: null,
  waveformSignals: [],
  waveforms: {
    yAxis: {
      unit: 'FS',
      minimum: -1,
      maximum: 1,
      ticks: [-1, 0, 1],
    },
  },
  onComparisonTargetsSwap: vi.fn(),
  onLayoutModeChange: vi.fn(),
  onRecordingGroupAssignment: vi.fn(),
  onRecordingToggle: vi.fn(),
  onSignalSelection: vi.fn(),
  onSignalChartModeChange: vi.fn(),
  onSpectrumPresetChange: vi.fn(),
  onSpectrumRangeEndChange: vi.fn(),
  onSpectrumRangeReset: vi.fn(),
  onSpectrumRangeStartChange: vi.fn(),
  onSurfaceChange: vi.fn(),
  onRegionOfInterestChange: vi.fn(),
  regionOfInterest: null as IAnalysisRegionOfInterest | null,
})

describe('TimeWaveformWorkspace', () => {
  beforeEach(() => {
    mockGetRecordingComparison.mockReset()
    mockExportComparisonReportMarkdown.mockReset()
    mockExportComparisonReportPdf.mockReset()
    mockDownloadBlobFile.mockReset()
    mockDownloadTextFile.mockReset()
    mockUseAnalysisWorkspacePanels.mockReturnValue({
      hasActiveChart: true,
      panels,
    })

    mockUseAnalysisWorkspaceMetrics.mockReturnValue({
      hasMetricsPending: false,
      metricSignals,
    })

    mockExportReportMarkdown.mockResolvedValue({
      fileName: 'soundlens-export-1-recording-20260710-120000.md',
      markdown: '# SoundLens export - 1 recording',
    })
    mockExportComparisonReportMarkdown.mockResolvedValue({
      fileName: 'alpha-vs-beta-comparison.md',
      markdown: '# Alpha vs beta comparison',
    })
    mockExportComparisonReportPdf.mockResolvedValue({
      fileName: 'alpha-vs-beta-comparison.pdf',
      pdf: new Blob(['%PDF-test'], { type: 'application/pdf' }),
    })
    mockGetRecordingComparison.mockResolvedValue(comparisonResponse)

    mockUseTimeWaveformWorkspace.mockReturnValue(createWorkspaceState())
  })

  it('shows the selected ROI summary and clears it from the workspace shell', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
      durationSeconds: 0.3,
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(screen.getByLabelText('Selected time region')).toBeInTheDocument()
    expect(screen.getByText('Selected region')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Clear region' }))

    expect(workspaceState.onRegionOfInterestChange).toHaveBeenCalledWith(null)
  })

  it('folds ROI scope into compare guidance and clears it there', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
    }
    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
      durationSeconds: 0.3,
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(mockGetRecordingComparison).toHaveBeenCalledWith('recording-1', 'recording-2', {
        startTimeSeconds: 0.1,
        endTimeSeconds: 0.4,
      })
    })

    expect(screen.getByLabelText('Comparison scope')).toHaveTextContent('ROI')
    expect(screen.getByText('0.10 s to 0.40 s · 0.30 s')).toBeInTheDocument()
    expect(screen.queryByLabelText('Selected time region')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Clear selected comparison region' }))

    expect(workspaceState.onRegionOfInterestChange).toHaveBeenCalledWith(null)
  })

  it('clamps a compare ROI to the shorter active recording before storing it', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 2.6,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2048,
        durationSeconds: 2.5350113378684807,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
    }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Select test region' }))

    expect(workspaceState.onRegionOfInterestChange).toHaveBeenCalledWith({
      startTimeSeconds: 1.5,
      endTimeSeconds: 2.5350113378684807,
      durationSeconds: 1.0350113378684807,
    })
  })

  it('shows the current comparison scope and forwards recording assignments', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(screen.getByLabelText('Comparison setup guidance')).toHaveTextContent('Incomplete')
    expect(screen.getByText('Choose the empty target.')).toBeInTheDocument()
    expect(screen.getByText('Compare disabled')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Assign recording' }))

    expect(workspaceState.onRecordingGroupAssignment).toHaveBeenCalledWith('recording-1', 'A')
  })

  it('enables compare mode without repeating a valid pair summary in the workspace', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(screen.queryByLabelText('Comparison setup guidance')).not.toBeInTheDocument()
    expect(screen.queryByText('Pair selected.')).not.toBeInTheDocument()
    expect(screen.getByText('Compare enabled')).toBeInTheDocument()
  })

  it('forwards an atomic pair swap and requests the reversed pair with the same ROI', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.2,
      endTimeSeconds: 0.8,
      durationSeconds: 0.6,
    }
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    const { rerender } = render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(mockGetRecordingComparison).toHaveBeenCalledWith('recording-1', 'recording-2', {
        startTimeSeconds: 0.2,
        endTimeSeconds: 0.8,
      })
    })

    fireEvent.click(screen.getByRole('button', { name: 'Swap pair' }))
    expect(workspaceState.onComparisonTargetsSwap).toHaveBeenCalledOnce()

    workspaceState.recordingGroupAssignments = {
      'recording-1': 'B',
      'recording-2': 'A',
    }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)
    rerender(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(mockGetRecordingComparison).toHaveBeenCalledWith('recording-2', 'recording-1', {
        startTimeSeconds: 0.2,
        endTimeSeconds: 0.8,
      })
    })
  })

  it('renders comparison metrics in backend order and preserves the selected metric across ROI refreshes', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    const { rerender } = render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(mockGetRecordingComparison).toHaveBeenCalledWith('recording-1', 'recording-2', null)
    })

    expect(screen.getByLabelText('Comparison metrics')).toBeInTheDocument()
    expect(screen.getByText('Comparison metrics', { selector: 'h3' })).toBeInTheDocument()
    expect(await screen.findByText('Weak evidence')).toBeInTheDocument()
    expect(screen.getByText('1 limitation')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Evidence & limitations' })).toHaveAttribute('aria-haspopup', 'dialog')
    const peakButton = screen.getByRole('button', { name: /Peak amplitude/i })
    const rmsButton = screen.getByRole('button', { name: /RMS amplitude/i })
    const crestButton = screen.getByRole('button', { name: /Crest factor/i })
    const clippingButton = screen.getByRole('button', { name: /Clipping samples/i })
    expect(peakButton.compareDocumentPosition(rmsButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(rmsButton.compareDocumentPosition(crestButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(crestButton.compareDocumentPosition(clippingButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()

    fireEvent.click(crestButton)

    expect(screen.getByRole('dialog', { name: 'Crest factor' })).toHaveTextContent('Mean delta A-B-0.400 ratio')
    expect(screen.getByRole('dialog', { name: 'Crest factor' })).toHaveTextContent('Low coverage')
    expect(crestButton).toHaveAttribute('aria-pressed', 'true')
    expect(peakButton.compareDocumentPosition(rmsButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    fireEvent.pointerDown(rmsButton, { button: 0, pointerType: 'mouse' })
    fireEvent.click(rmsButton)
    expect(screen.getByRole('dialog', { name: 'RMS amplitude' })).toBeInTheDocument()
    expect(rmsButton).toHaveAttribute('aria-pressed', 'true')
    expect(screen.queryByRole('button', { name: 'Hide evidence' })).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Close evidence inspector' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Evidence & limitations' }))
    expect(screen.getByRole('dialog', { name: 'RMS amplitude' })).toBeInTheDocument()

    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.2,
      endTimeSeconds: 0.8,
      durationSeconds: 0.6,
    }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)
    rerender(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    await waitFor(() => {
      expect(mockGetRecordingComparison).toHaveBeenCalledWith('recording-1', 'recording-2', {
        startTimeSeconds: 0.2,
        endTimeSeconds: 0.8,
      })
    })
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Evidence & limitations' }))
    expect(screen.getByRole('dialog', { name: 'RMS amplitude' })).toHaveTextContent('ROI 0.20 s to 0.80 s · 0.60 s')
  })

  it('closes Copilot before opening comparison evidence', async () => {
    const workspaceState = createWorkspaceState()
    const onCopilotToggle = vi.fn()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1_024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
    }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    const { rerender } = render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen
        onCopilotToggle={onCopilotToggle}
      />
    )

    const evidenceButton = await screen.findByRole('button', { name: 'Evidence & limitations' })
    fireEvent.click(evidenceButton)

    expect(onCopilotToggle).toHaveBeenCalledTimes(1)
    expect(screen.getByRole('dialog', { name: 'Peak amplitude' })).toBeInTheDocument()

    workspaceState.recordings = [...workspaceState.recordings]
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)
    rerender(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={onCopilotToggle}
      />
    )

    expect(screen.getByRole('dialog', { name: 'Peak amplitude' })).toBeInTheDocument()
    expect(onCopilotToggle).toHaveBeenCalledTimes(1)
    expect(mockGetRecordingComparison).toHaveBeenCalledTimes(1)
  })

  it('opens the recording drawer while closing Copilot in one action', () => {
    const onCopilotToggle = vi.fn()

    mockUseTimeWaveformWorkspace.mockReturnValue(createWorkspaceState())
    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen
        onCopilotToggle={onCopilotToggle}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Open recordings drawer' }))

    expect(onCopilotToggle).toHaveBeenCalledOnce()
    expect(screen.getByText('Recording drawer open')).toBeInTheDocument()
  })

  it('keeps Copilot open when a metric card changes its grounded context', async () => {
    const workspaceState = createWorkspaceState()
    const onCopilotToggle = vi.fn()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1_024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
    }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen
        onCopilotToggle={onCopilotToggle}
      />
    )

    const crestButton = await screen.findByRole('button', { name: /Crest factor/i })
    fireEvent.click(crestButton)

    expect(crestButton).toHaveAttribute('aria-pressed', 'true')
    expect(onCopilotToggle).not.toHaveBeenCalled()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('blocks comparison when inconsistent state assigns more than one recording to a target', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-2',
        fileName: 'beta.wav',
        sizeBytes: 2_048,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
      {
        recordingId: 'recording-3',
        fileName: 'gamma.wav',
        sizeBytes: 3_072,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'A',
      'recording-3': 'B',
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(screen.getByLabelText('Comparison setup guidance')).toHaveTextContent('Resolve pair')
    expect(screen.getByText('Choose exactly one recording for each target.')).toBeInTheDocument()
    expect(screen.queryByLabelText('Active comparison pair')).not.toBeInTheDocument()
    expect(screen.getByText('Compare disabled')).toBeInTheDocument()
    expect(mockGetRecordingComparison).not.toHaveBeenCalled()
  })

  it('keeps compare mode blocked when nothing is assigned yet', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [],
      },
    ]

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(screen.getByLabelText('Comparison setup guidance')).toHaveTextContent('Not ready')
    expect(screen.getByText('Choose A and B.')).toBeInTheDocument()
    expect(screen.getByText('Compare disabled')).toBeInTheDocument()
  })

  it('prefers spectrum-backed metrics whenever an ROI is active', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
      durationSeconds: 0.3,
    }
    workspaceState.spectrumSignals = [
      {
        signalId: 'signal-1',
        recordingId: 'recording-1',
        recordingFileName: 'alpha.wav',
        displayName: 'Channel 1',
        durationSeconds: 1,
        sampleRate: 44_100,
        channelIndex: 0,
        amplitudeUnit: 'dB rel.',
        isCalibrated: false,
        metrics: undefined,
        findings: [],
        points: [],
      },
    ]

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(mockUseAnalysisWorkspaceMetrics).toHaveBeenCalledWith(
      expect.objectContaining({
        preferSpectrumMetrics: true,
        spectrumSignals: workspaceState.spectrumSignals,
      })
    )
  })

  it('exports the current workspace context from the header action', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.recordings = [
      {
        recordingId: 'recording-1',
        fileName: 'alpha.wav',
        sizeBytes: 1024,
        durationSeconds: 1,
        sampleRate: 44_100,
        channels: 1,
        channelMode: 'Mono',
        signals: [
          {
            signalId: 'signal-1',
            channelIndex: 0,
            displayName: 'Channel 1',
          },
        ],
      },
    ]
    workspaceState.selectedSignalIds = ['signal-1']
    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
      durationSeconds: 0.3,
    }

    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace
        importedRecordingCount={importedFiles.length}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    fireEvent.click(screen.getByRole('button', { name: 'Export report' }))

    await waitFor(() => {
    expect(mockExportReportMarkdown).toHaveBeenCalledWith({
      activeSurface: 'waveform',
      layoutMode: 'focused',
      signalChartMode: 'overlay',
        recordings: [
          {
            recordingId: 'recording-1',
            fileName: 'alpha.wav',
            sizeBytes: 1024,
            durationSeconds: 1,
            sampleRate: 44_100,
            channels: 1,
            channelMode: 'Mono',
            signals: [
              {
                signalId: 'signal-1',
                channelIndex: 0,
                displayName: 'Channel 1',
                fileName: 'alpha.wav',
              },
            ],
        },
      ],
      selectedSignalEvidence: [],
      selectedSignalIds: ['signal-1'],
      startTimeSeconds: 0.1,
      endTimeSeconds: 0.4,
    })
      expect(mockDownloadTextFile).toHaveBeenCalledWith(
        'soundlens-export-1-recording-20260710-120000.md',
        '# SoundLens export - 1 recording'
      )
    })
  })

  it('previews and exports a valid comparison with scope and exclusions', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      createRecording('recording-1', 'alpha.wav'),
      createRecording('recording-2', 'beta.wav'),
      createRecording('recording-3', 'gamma.wav'),
    ]
    workspaceState.recordingGroupAssignments = {
      'recording-1': 'A',
      'recording-2': 'B',
      'recording-3': 'unassigned',
    }
    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.2,
      endTimeSeconds: 0.8,
      durationSeconds: 0.6,
    }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace importedRecordingCount={importedFiles.length} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
    )

    await waitFor(() => expect(screen.getByRole('button', { name: 'Export report' })).toBeEnabled())
    fireEvent.click(screen.getByRole('button', { name: 'Export report' }))

    expect(screen.getByRole('dialog', { name: 'Export comparison report' })).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: 'Report title' })).toHaveValue('alpha.wav vs beta.wav comparison')
    expect(screen.getByText('gamma.wav')).toBeInTheDocument()
    expect(screen.getByText('0.20 s to 0.80 s')).toBeInTheDocument()

    fireEvent.change(screen.getByRole('textbox', { name: 'Report title' }), {
      target: { value: 'Edited comparison title' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Export Markdown' }))

    await waitFor(() => {
      expect(mockExportComparisonReportMarkdown).toHaveBeenCalledWith({
        reportTitle: 'Edited comparison title',
        recordingIdA: 'recording-1',
        recordingIdB: 'recording-2',
        metricKey: 'peakAmplitudeDelta',
        signalIdA: 'signal-a',
        signalIdB: 'signal-b',
        excludedRecordings: [{ recordingId: 'recording-3', assignment: 'unassigned' }],
        startTimeSeconds: 0.2,
        endTimeSeconds: 0.8,
      })
      expect(mockDownloadTextFile).toHaveBeenCalledWith(
        'alpha-vs-beta-comparison.md',
        '# Alpha vs beta comparison'
      )
    })
    expect(screen.queryByRole('dialog', { name: 'Export comparison report' })).not.toBeInTheDocument()
  })

  it('keeps compare export unavailable until valid comparison evidence exists', () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [createRecording('recording-1', 'alpha.wav')]
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace importedRecordingCount={importedFiles.length} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
    )

    expect(screen.getByRole('button', { name: 'Export report' })).toBeDisabled()
    expect(screen.queryByRole('dialog', { name: 'Export comparison report' })).not.toBeInTheDocument()
  })

  it('exports the same backend-owned comparison request as PDF', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      createRecording('recording-1', 'alpha.wav'),
      createRecording('recording-2', 'beta.wav'),
    ]
    workspaceState.recordingGroupAssignments = { 'recording-1': 'A', 'recording-2': 'B' }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)

    render(
      <TimeWaveformWorkspace importedRecordingCount={importedFiles.length} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
    )

    await waitFor(() => expect(screen.getByRole('button', { name: 'Export report' })).toBeEnabled())
    fireEvent.click(screen.getByRole('button', { name: 'Export report' }))
    expect(screen.getByRole('radio', { name: /Markdown/ })).toBeChecked()

    fireEvent.click(screen.getByRole('radio', { name: /PDF/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Export PDF' }))

    await waitFor(() => {
      expect(mockExportComparisonReportPdf).toHaveBeenCalledWith({
        reportTitle: 'alpha.wav vs beta.wav comparison',
        recordingIdA: 'recording-1',
        recordingIdB: 'recording-2',
        metricKey: 'peakAmplitudeDelta',
        signalIdA: 'signal-a',
        signalIdB: 'signal-b',
        excludedRecordings: [],
        startTimeSeconds: undefined,
        endTimeSeconds: undefined,
      })
      expect(mockDownloadBlobFile).toHaveBeenCalledWith(
        'alpha-vs-beta-comparison.pdf',
        expect.any(Blob)
      )
    })
    expect(mockExportComparisonReportMarkdown).not.toHaveBeenCalled()
    expect(screen.queryByRole('dialog', { name: 'Export comparison report' })).not.toBeInTheDocument()
  })

  it('keeps the comparison preview open when export fails', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      createRecording('recording-1', 'alpha.wav'),
      createRecording('recording-2', 'beta.wav'),
    ]
    workspaceState.recordingGroupAssignments = { 'recording-1': 'A', 'recording-2': 'B' }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)
    mockExportComparisonReportMarkdown.mockRejectedValue(new Error('Export failed'))

    render(
      <TimeWaveformWorkspace importedRecordingCount={importedFiles.length} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
    )

    await waitFor(() => expect(screen.getByRole('button', { name: 'Export report' })).toBeEnabled())
    fireEvent.click(screen.getByRole('button', { name: 'Export report' }))
    fireEvent.click(screen.getByRole('button', { name: 'Export Markdown' }))

    await waitFor(() => expect(mockExportComparisonReportMarkdown).toHaveBeenCalled())
    expect(screen.getByRole('dialog', { name: 'Export comparison report' })).toBeInTheDocument()
    expect(mockDownloadTextFile).not.toHaveBeenCalled()
  })

  it('keeps the comparison preview open when PDF export fails', async () => {
    const workspaceState = createWorkspaceState()
    workspaceState.layoutMode = 'compare'
    workspaceState.recordings = [
      createRecording('recording-1', 'alpha.wav'),
      createRecording('recording-2', 'beta.wav'),
    ]
    workspaceState.recordingGroupAssignments = { 'recording-1': 'A', 'recording-2': 'B' }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)
    mockExportComparisonReportPdf.mockRejectedValue(new Error('PDF export failed'))

    render(
      <TimeWaveformWorkspace importedRecordingCount={importedFiles.length} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
    )

    await waitFor(() => expect(screen.getByRole('button', { name: 'Export report' })).toBeEnabled())
    fireEvent.click(screen.getByRole('button', { name: 'Export report' }))
    fireEvent.click(screen.getByRole('radio', { name: /PDF/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Export PDF' }))

    await waitFor(() => expect(mockExportComparisonReportPdf).toHaveBeenCalled())
    expect(screen.getByRole('dialog', { name: 'Export comparison report' })).toBeInTheDocument()
    expect(mockDownloadBlobFile).not.toHaveBeenCalled()
  })
})

const createRecording = (recordingId: string, fileName: string): ITimeWaveformRecording => ({
  recordingId,
  fileName,
  sizeBytes: 1024,
  durationSeconds: 1,
  sampleRate: 44_100,
  channels: 1,
  channelMode: 'Mono',
  signals: [],
})
