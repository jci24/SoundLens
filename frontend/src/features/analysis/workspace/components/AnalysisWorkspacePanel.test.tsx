import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { AnalysisWorkspacePanel } from './AnalysisWorkspacePanel'
import type { IFrequencySpectrumAxis, ITimeWaveformAxis, ITimeWaveformSignal } from '../../types'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'

vi.mock('../../waveform/components/WaveformChart', () => ({
  WaveformChart: ({ signals, width }: { signals: ITimeWaveformSignal[]; width: number }) => (
    <div data-testid="waveform-chart">{`${signals[0]?.displayName ?? 'none'}:${width}`}</div>
  ),
}))

vi.mock('../../spectrum/components/SpectrumChart', () => ({
  SpectrumChart: () => <div data-testid="spectrum-chart">spectrum-chart</div>,
}))

vi.mock('../hooks/useMeasuredChartWidth', () => ({
  useMeasuredChartWidth: () => 480,
}))

const waveformYAxis: ITimeWaveformAxis = {
  unit: 'FS',
  minimum: -1,
  maximum: 1,
  ticks: [-1, 0, 1],
}

const spectrumAxis: IFrequencySpectrumAxis = {
  unit: 'Hz',
  minimum: 0,
  maximum: 22_050,
  ticks: [0, 11_025, 22_050],
}

const waveformSignals: ITimeWaveformSignal[] = [
  {
    signalId: 'signal-left',
    recordingId: 'recording-1',
    recordingFileName: 'alpha.wav',
    displayName: 'Left',
    durationSeconds: 1,
    sampleRate: 44_100,
    channelIndex: 0,
    amplitudeUnit: 'FS',
    isCalibrated: false,
    metrics: undefined,
    findings: [],
    bins: [],
  },
  {
    signalId: 'signal-right',
    recordingId: 'recording-1',
    recordingFileName: 'alpha.wav',
    displayName: 'Right',
    durationSeconds: 1,
    sampleRate: 44_100,
    channelIndex: 1,
    amplitudeUnit: 'FS',
    isCalibrated: false,
    metrics: undefined,
    findings: [],
    bins: [],
  },
]

const basePanel: IAnalysisWorkspacePanel = {
  surface: 'waveform',
  title: 'Waveform',
  isInitialLoading: false,
  isRefreshing: false,
  loadingLabel: 'Loading waveform…',
  refreshingLabel: 'Refreshing waveform…',
  error: null,
}

describe('AnalysisWorkspacePanel', () => {
  it('renders a loading state before the chart is ready', () => {
    render(
      <AnalysisWorkspacePanel
        chartWidth={640}
        isCompareMode={false}
        onRegionOfInterestChange={vi.fn()}
        panel={{ ...basePanel, isInitialLoading: true }}
        regionOfInterest={null}
        signalChartMode="overlay"
        spectrumSignals={[]}
        spectrumXAxis={spectrumAxis}
        spectrumYAxis={spectrumAxis}
        waveformSignals={waveformSignals}
        waveformYAxis={waveformYAxis}
      />
    )

    expect(screen.getByText('Loading waveform…')).toBeInTheDocument()
    expect(screen.getByTestId('waveform-chart')).toBeInTheDocument()
  })

  it('renders an error state when analysis fails', () => {
    render(
      <AnalysisWorkspacePanel
        chartWidth={640}
        isCompareMode={false}
        onRegionOfInterestChange={vi.fn()}
        panel={{ ...basePanel, error: 'Waveform request failed' }}
        regionOfInterest={null}
        signalChartMode="overlay"
        spectrumSignals={[]}
        spectrumXAxis={spectrumAxis}
        spectrumYAxis={spectrumAxis}
        waveformSignals={waveformSignals}
        waveformYAxis={waveformYAxis}
      />
    )

    expect(screen.getByText('Waveform request failed')).toBeInTheDocument()
    expect(screen.queryByTestId('waveform-chart')).not.toBeInTheDocument()
  })

  it('renders split charts and refresh feedback in focused mode', () => {
    render(
      <AnalysisWorkspacePanel
        chartWidth={640}
        isCompareMode={false}
        onRegionOfInterestChange={vi.fn()}
        panel={{ ...basePanel, isRefreshing: true }}
        regionOfInterest={null}
        signalChartMode="split"
        spectrumSignals={[]}
        spectrumXAxis={spectrumAxis}
        spectrumYAxis={spectrumAxis}
        waveformSignals={waveformSignals}
        waveformYAxis={waveformYAxis}
      />
    )

    expect(screen.getByText('Refreshing waveform…')).toBeInTheDocument()
    expect(screen.getByText('alpha.wav · Left')).toBeInTheDocument()
    expect(screen.getByText('alpha.wav · Right')).toBeInTheDocument()
    expect(screen.getAllByTestId('waveform-chart')).toHaveLength(2)
    expect(screen.getByText('Left:480')).toBeInTheDocument()
    expect(screen.getByText('Right:480')).toBeInTheDocument()
  })
})
