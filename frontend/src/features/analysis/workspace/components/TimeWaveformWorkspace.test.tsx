import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TimeWaveformWorkspace } from './TimeWaveformWorkspace'
import type { IMetricSignalItem } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import type { IAnalysisRegionOfInterest, IFrequencySpectrumSignal } from '../../types'

const mockUseTimeWaveformWorkspace = vi.fn()
const mockUseAnalysisWorkspacePanels = vi.fn()
const mockUseAnalysisWorkspaceMetrics = vi.fn()

vi.mock('../hooks/useTimeWaveformWorkspace', () => ({
  useTimeWaveformWorkspace: (...args: unknown[]) => mockUseTimeWaveformWorkspace(...args),
}))

vi.mock('../hooks/useAnalysisWorkspacePanels', () => ({
  useAnalysisWorkspacePanels: (...args: unknown[]) => mockUseAnalysisWorkspacePanels(...args),
}))

vi.mock('../../metrics/hooks/useAnalysisWorkspaceMetrics', () => ({
  useAnalysisWorkspaceMetrics: (...args: unknown[]) => mockUseAnalysisWorkspaceMetrics(...args),
}))

vi.mock('./AnalysisWorkspaceHeader', () => ({
  AnalysisWorkspaceHeader: () => <div data-testid="workspace-header" />,
}))

vi.mock('../../recording-rail/components/RecordingRail', () => ({
  RecordingRail: () => <div data-testid="recording-rail" />,
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
  recordings: [],
  selectedSignalIds: [],
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
})
