import { createRef } from 'react'
import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { AnalysisWorkspaceChart } from './AnalysisWorkspaceChart'
import type { IMetricSignalItem } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'
import type { IFrequencySpectrumAxis, ITimeWaveformAxis } from '../../types'

vi.mock('./AnalysisWorkspacePanel', () => ({
  AnalysisWorkspacePanel: ({ panel }: { panel: IAnalysisWorkspacePanel }) => (
    <div data-testid={`panel-${panel.surface}`}>{panel.title}</div>
  ),
}))

const metricSignals: IMetricSignalItem[] = [
  {
    signalId: 'signal-1',
    recordingFileName: 'alpha.wav',
    displayName: 'Channel 1',
    durationSeconds: 1.23,
    sampleRate: 44_100,
    peakAmplitude: 0.82,
    rmsAmplitude: 0.41,
    crestFactor: 2,
    clippingSampleCount: 3,
    hasClipping: true,
    findings: [
      {
        category: 'Clipping',
        severity: 'Alert',
        label: 'Clipping detected',
        detail: '3 samples clipped',
      },
    ],
  },
]

const panels: IAnalysisWorkspacePanel[] = [
  {
    surface: 'waveform',
    title: 'Waveform',
    isInitialLoading: false,
    isRefreshing: false,
    loadingLabel: 'Loading waveform…',
    refreshingLabel: 'Refreshing waveform…',
    error: null,
  },
]

const spectrumAxis: IFrequencySpectrumAxis = {
  unit: 'Hz',
  minimum: 0,
  maximum: 22_050,
  ticks: [0, 11_025, 22_050],
}

const waveformYAxis: ITimeWaveformAxis = {
  unit: 'FS',
  minimum: -1,
  maximum: 1,
  ticks: [-1, 0, 1],
}

describe('AnalysisWorkspaceChart', () => {
  it('renders metrics, deterministic findings, and panel composition together', () => {
    render(
      <AnalysisWorkspaceChart
        chartRef={createRef<HTMLDivElement>()}
        chartWidth={720}
        compareEvidenceDetail="Δ -1.121 ratio · A 6.784 ratio · B 5.664 ratio"
        compareEvidenceKicker="Inspecting evidence for"
        compareEvidenceScope="ROI 0.84 s to 1.94 s · 1.09 s"
        compareEvidenceSummary="Channel 1 vs Channel 1"
        compareEvidenceTitle="Crest factor"
        hasMetricsPending={true}
        isCompareMode={true}
        metricSignals={metricSignals}
        onRegionOfInterestChange={vi.fn()}
        panels={panels}
        regionOfInterest={null}
        signalChartMode="overlay"
        spectrumSignals={[]}
        spectrumXAxis={spectrumAxis}
        spectrumYAxis={spectrumAxis}
        waveformSignals={[]}
        waveformYAxis={waveformYAxis}
      />
    )

    expect(screen.getByLabelText('Signal metrics table')).toBeInTheDocument()
    expect(screen.getByText('Restart backend to load live metrics')).toBeInTheDocument()
    expect(screen.getByLabelText('Signal findings')).toBeInTheDocument()
    expect(screen.getByText('Clipping detected')).toBeInTheDocument()
    expect(screen.getByText('3 samples clipped')).toBeInTheDocument()
    expect(screen.getByLabelText('Chart evidence context')).toHaveTextContent('Inspecting evidence for')
    expect(screen.getByLabelText('Chart evidence context')).toHaveTextContent('Crest factor')
    expect(screen.getByText('Channel 1 vs Channel 1')).toBeInTheDocument()
    expect(screen.getByText('Δ -1.121 ratio · A 6.784 ratio · B 5.664 ratio')).toBeInTheDocument()
    expect(screen.getByText('ROI 0.84 s to 1.94 s · 1.09 s')).toBeInTheDocument()
    expect(screen.getByTestId('panel-waveform')).toHaveTextContent('Waveform')
  })
})
