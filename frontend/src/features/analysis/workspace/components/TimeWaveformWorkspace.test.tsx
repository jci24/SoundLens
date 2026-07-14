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

vi.mock('../../report/utils/reportDownload', () => ({
  downloadTextFile: (...args: unknown[]) => mockDownloadTextFile(...args),
}))

vi.mock('../../services/recordingComparison', () => ({
  getRecordingComparison: (...args: unknown[]) => mockGetRecordingComparison(...args),
}))

vi.mock('./AnalysisWorkspaceHeader', () => ({
  AnalysisWorkspaceHeader: ({
    canExportReport,
    canEnterCompareMode,
    onExportReport,
  }: {
    canExportReport: boolean
    canEnterCompareMode: boolean
    onExportReport: () => void
  }) => (
    <div>
      <button data-testid="workspace-header" disabled={!canExportReport} onClick={onExportReport} type="button">
        Export report
      </button>
      <span>{canEnterCompareMode ? 'Compare enabled' : 'Compare disabled'}</span>
    </div>
  ),
}))

vi.mock('../../recording-rail/components/RecordingRail', () => ({
  RecordingRail: ({
    onRecordingGroupAssignment,
    recordingGroupAssignments,
  }: {
    onRecordingGroupAssignment: (recordingId: string, assignment: 'A' | 'B' | 'unassigned') => void
    recordingGroupAssignments: Record<string, 'A' | 'B' | 'unassigned'>
  }) => (
    <div data-testid="recording-rail">
      <button onClick={() => onRecordingGroupAssignment('recording-1', 'A')} type="button">
        Assign recording
      </button>
      <span>{recordingGroupAssignments['recording-1'] ?? 'unassigned'}</span>
    </div>
  ),
}))

vi.mock('./AnalysisWorkspaceChart', () => ({
  AnalysisWorkspaceChart: () => <div data-testid="workspace-chart" />,
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
        importedFiles={importedFiles}
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
        importedFiles={importedFiles}
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
        importedFiles={importedFiles}
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

  it('marks compare mode ready when both groups are populated', () => {
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
        importedFiles={importedFiles}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(screen.getByLabelText('Comparison setup guidance')).toHaveTextContent('Ready')
    expect(screen.getByText('A 1 · B 1')).toBeInTheDocument()
    expect(screen.getByText('Compare enabled')).toBeInTheDocument()
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
        importedFiles={importedFiles}
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
    expect(screen.queryByLabelText('Selected metric evidence')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Comparison limitations')).not.toBeInTheDocument()
    const peakButton = screen.getByRole('button', { name: /Peak amplitude/i })
    const rmsButton = screen.getByRole('button', { name: /RMS amplitude/i })
    const crestButton = screen.getByRole('button', { name: /Crest factor/i })
    const clippingButton = screen.getByRole('button', { name: /Clipping samples/i })
    expect(peakButton.compareDocumentPosition(rmsButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(rmsButton.compareDocumentPosition(crestButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(crestButton.compareDocumentPosition(clippingButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()

    fireEvent.click(crestButton)
    fireEvent.click(screen.getByRole('button', { name: 'Details' }))

    expect(screen.getByLabelText('Selected metric evidence')).toHaveTextContent('Crest factor')
    expect(peakButton.compareDocumentPosition(rmsButton) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(screen.getByLabelText('Comparison limitations')).toHaveTextContent('Low coverage')

    workspaceState.regionOfInterest = {
      startTimeSeconds: 0.2,
      endTimeSeconds: 0.8,
      durationSeconds: 0.6,
    }
    mockUseTimeWaveformWorkspace.mockReturnValue(workspaceState)
    rerender(
      <TimeWaveformWorkspace
        importedFiles={importedFiles}
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
    fireEvent.click(screen.getByRole('button', { name: 'Details' }))
    expect(screen.getByLabelText('Selected metric evidence')).toHaveTextContent('Crest factor')
  })

  it('shows a pairwise-only state when more than one recording is assigned to a group', () => {
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
        importedFiles={importedFiles}
        isCopilotOpen={false}
        onCopilotToggle={vi.fn()}
      />
    )

    expect(screen.getByLabelText('Comparison setup guidance')).toHaveTextContent('Ready')
    expect(screen.getByText('Using the first recording on each side.')).toBeInTheDocument()
    expect(screen.getByLabelText('Active comparison pair')).toHaveTextContent('alpha.wav')
    expect(screen.getByLabelText('Active comparison pair')).toHaveTextContent('gamma.wav')
    expect(screen.getByLabelText('Queued comparison recordings')).toHaveTextContent('A waiting: beta.wav')
    expect(screen.getByLabelText('Comparison metrics')).toHaveTextContent(
      'This slice compares one pair at a time. Now using alpha.wav vs gamma.wav. A waiting: beta.wav.'
    )
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
        importedFiles={importedFiles}
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
        importedFiles={importedFiles}
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
        importedFiles={importedFiles}
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
      <TimeWaveformWorkspace importedFiles={importedFiles} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
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
      <TimeWaveformWorkspace importedFiles={importedFiles} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
    )

    expect(screen.getByRole('button', { name: 'Export report' })).toBeDisabled()
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
      <TimeWaveformWorkspace importedFiles={importedFiles} isCopilotOpen={false} onCopilotToggle={vi.fn()} />
    )

    await waitFor(() => expect(screen.getByRole('button', { name: 'Export report' })).toBeEnabled())
    fireEvent.click(screen.getByRole('button', { name: 'Export report' }))
    fireEvent.click(screen.getByRole('button', { name: 'Export Markdown' }))

    await waitFor(() => expect(mockExportComparisonReportMarkdown).toHaveBeenCalled())
    expect(screen.getByRole('dialog', { name: 'Export comparison report' })).toBeInTheDocument()
    expect(mockDownloadTextFile).not.toHaveBeenCalled()
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
