import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TimeWaveformWorkspace } from './TimeWaveformWorkspace'
import type { IMetricSignalItem } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import type { IAnalysisRegionOfInterest, IFrequencySpectrumSignal, ITimeWaveformRecording } from '../../types'

const mockUseTimeWaveformWorkspace = vi.fn()
const mockUseAnalysisWorkspacePanels = vi.fn()
const mockUseAnalysisWorkspaceMetrics = vi.fn()
const mockExportReportMarkdown = vi.fn()
const mockDownloadTextFile = vi.fn()

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
  exportReportMarkdown: (...args: unknown[]) => mockExportReportMarkdown(...args),
}))

vi.mock('../../report/utils/reportDownload', () => ({
  downloadTextFile: (...args: unknown[]) => mockDownloadTextFile(...args),
}))

vi.mock('./AnalysisWorkspaceHeader', () => ({
  AnalysisWorkspaceHeader: ({ onExportReport }: { onExportReport: () => void }) => (
    <button data-testid="workspace-header" onClick={onExportReport} type="button">
      Export report
    </button>
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

const createWorkspaceState = () => ({
  activeSurface: 'waveform' as const,
  chartRef: { current: null },
  chartWidth: 720,
  expandedRecordings: [],
  isSpectrumInitialLoading: false,
  isSpectrumRefreshing: false,
  isWaveformInitialLoading: false,
  isWaveformRefreshing: false,
  layoutMode: 'focused' as const,
  recordings: [] as ITimeWaveformRecording[],
  recordingGroupAssignments: {} as Record<string, 'A' | 'B' | 'unassigned'>,
  selectedSignalIds: [] as string[],
  selectedSpectrumPreset: '4096',
  signalChartMode: 'overlay' as const,
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

    const comparisonScope = screen.getByLabelText('Comparison scope')

    expect(comparisonScope).toBeInTheDocument()
    expect(screen.getByText('Setup')).toBeInTheDocument()
    expect(screen.getByText('Comparison scope')).toBeInTheDocument()
    expect(comparisonScope).toHaveTextContent('A 1')
    expect(comparisonScope).toHaveTextContent('B 0')
    expect(comparisonScope).toHaveTextContent('Unassigned 1')

    fireEvent.click(screen.getByRole('button', { name: 'Assign recording' }))

    expect(workspaceState.onRecordingGroupAssignment).toHaveBeenCalledWith('recording-1', 'A')
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
})
