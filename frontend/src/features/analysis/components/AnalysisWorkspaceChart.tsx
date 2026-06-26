import type { RefObject } from 'react'
import { AnalysisWorkspaceMetricsRail } from './AnalysisWorkspaceMetricsRail'
import { AnalysisWorkspacePanel } from './AnalysisWorkspacePanel'
import type { IMetricSignalItem } from '../hooks/useAnalysisWorkspaceMetrics'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'
import type { TSignalChartMode } from '../hooks/useTimeWaveformWorkspace'
import type { IFrequencySpectrumAxis, IFrequencySpectrumSignal, ITimeWaveformSignal, ITimeWaveformResponse } from '../types'

interface IAnalysisWorkspaceChartProps {
  chartRef: RefObject<HTMLDivElement | null>
  chartWidth: number
  hasMetricsPending: boolean
  isCompareMode: boolean
  metricSignals: IMetricSignalItem[]
  panels: IAnalysisWorkspacePanel[]
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
  hasMetricsPending,
  isCompareMode,
  metricSignals,
  panels,
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

    <div
      className={`time-waveform-workspace__panel-grid${isCompareMode ? ' time-waveform-workspace__panel-grid--compare' : ''}`}
      ref={chartRef}
    >
      {panels.map((panel) => (
        <AnalysisWorkspacePanel
          chartWidth={chartWidth}
          isCompareMode={isCompareMode}
          key={panel.surface}
          panel={panel}
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
