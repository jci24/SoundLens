import type { RefObject } from 'react'
import { AnalysisWorkspaceMetricsRail } from '../../metrics/components/AnalysisWorkspaceMetricsRail'
import { AnalysisWorkspacePanel } from './AnalysisWorkspacePanel'
import { FindingsPanel } from '../../metrics/components/FindingsPanel'
import type { IMetricSignalItem } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'
import type { IAnalysisRegionOfInterest, TSignalChartMode, IFrequencySpectrumAxis, IFrequencySpectrumSignal, ITimeWaveformSignal, ITimeWaveformResponse } from '../../types'

interface IAnalysisWorkspaceChartProps {
  chartRef: RefObject<HTMLDivElement | null>
  chartWidth: number
  compareEvidenceSummary: string | null
  compareEvidenceTitle: string | null
  hasMetricsPending: boolean
  isCompareMode: boolean
  metricSignals: IMetricSignalItem[]
  onRegionOfInterestChange: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  panels: IAnalysisWorkspacePanel[]
  regionOfInterest: IAnalysisRegionOfInterest | null
  signalChartMode: TSignalChartMode
  spectrumSignals: IFrequencySpectrumSignal[]
  spectrumXAxis: IFrequencySpectrumAxis | null
  spectrumYAxis: IFrequencySpectrumAxis | null
  waveformSignals: ITimeWaveformSignal[]
  waveformYAxis: ITimeWaveformResponse['yAxis'] | null
}

const AnalysisWorkspaceChart = ({
  chartRef,
  chartWidth,
  compareEvidenceSummary,
  compareEvidenceTitle,
  hasMetricsPending,
  isCompareMode,
  metricSignals,
  onRegionOfInterestChange,
  panels,
  regionOfInterest,
  signalChartMode,
  spectrumSignals,
  spectrumXAxis,
  spectrumYAxis,
  waveformSignals,
  waveformYAxis,
}: IAnalysisWorkspaceChartProps) => (
  <div className="time-waveform-workspace__visual-stack">
    {metricSignals.length > 0 && (
      <AnalysisWorkspaceMetricsRail
        hasMetricsPending={hasMetricsPending}
        signals={metricSignals}
      />
    )}
    <FindingsPanel signals={metricSignals} />
    {isCompareMode && compareEvidenceTitle && (
      <section className="time-waveform-workspace__evidence-bridge" aria-label="Chart evidence context">
        <strong className="time-waveform-workspace__evidence-bridge-title">{compareEvidenceTitle}</strong>
        {compareEvidenceSummary && (
          <p className="time-waveform-workspace__evidence-bridge-copy">{compareEvidenceSummary}</p>
        )}
      </section>
    )}

    <div
      className={`time-waveform-workspace__panel-grid${isCompareMode ? ' time-waveform-workspace__panel-grid--compare' : ''}`}
      ref={chartRef}
    >
      {panels.map((panel) => (
        <AnalysisWorkspacePanel
          chartWidth={chartWidth}
          isCompareMode={isCompareMode}
          key={panel.surface}
          onRegionOfInterestChange={onRegionOfInterestChange}
          panel={panel}
          regionOfInterest={regionOfInterest}
          signalChartMode={signalChartMode}
          spectrumSignals={spectrumSignals}
          spectrumXAxis={spectrumXAxis}
          spectrumYAxis={spectrumYAxis}
          waveformSignals={waveformSignals}
          waveformYAxis={waveformYAxis}
        />
      ))}
    </div>
  </div>
)

export { AnalysisWorkspaceChart }
