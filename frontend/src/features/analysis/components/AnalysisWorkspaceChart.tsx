import type { RefObject } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { AnalysisWorkspaceMetricsRail } from './AnalysisWorkspaceMetricsRail'
import { SpectrumChart } from './SpectrumChart'
import { WaveformChart } from './WaveformChart'
import type { IMetricSignalItem } from '../hooks/useAnalysisWorkspaceMetrics'
import type { IFrequencySpectrumAxis, IFrequencySpectrumSignal, ITimeWaveformSignal, ITimeWaveformResponse } from '../types'
import type { TAnalysisSurface } from '../hooks/useTimeWaveformWorkspace'

interface IAnalysisWorkspaceChartProps {
  activeSurface: TAnalysisSurface
  chartRef: RefObject<HTMLDivElement | null>
  chartWidth: number
  error: string | null
  isInitialLoading: boolean
  hasMetricsPending: boolean
  isRefreshing: boolean
  loadingLabel: string
  metricSignals: IMetricSignalItem[]
  refreshingLabel: string
  spectrumSignals: IFrequencySpectrumSignal[]
  spectrumXAxis: IFrequencySpectrumAxis | null
  spectrumYAxis: IFrequencySpectrumAxis | null
  waveformSignals: ITimeWaveformSignal[]
  waveformYAxis: ITimeWaveformResponse['yAxis'] | null
}

const AnalysisWorkspaceChart = ({
  activeSurface,
  chartRef,
  chartWidth,
  error,
  isInitialLoading,
  hasMetricsPending,
  isRefreshing,
  loadingLabel,
  metricSignals,
  refreshingLabel,
  spectrumSignals,
  spectrumXAxis,
  spectrumYAxis,
  waveformSignals,
  waveformYAxis,
}: IAnalysisWorkspaceChartProps) => (
  <div className="time-waveform-workspace__chart-shell" ref={chartRef}>
    {!error && metricSignals.length > 0 && (
      <AnalysisWorkspaceMetricsRail
        hasMetricsPending={hasMetricsPending}
        signals={metricSignals}
      />
    )}

    {isInitialLoading && (
      <div className="time-waveform-workspace__state">
        <Loader2 className="time-waveform-workspace__spinner" size={20} />
        <span>{loadingLabel}</span>
      </div>
    )}

    {isRefreshing && !isInitialLoading && (
      <div className="time-waveform-workspace__loading-pill" aria-live="polite">
        <Loader2 className="time-waveform-workspace__spinner" size={14} />
        <span>{refreshingLabel}</span>
      </div>
    )}

    {error && (
      <div className="time-waveform-workspace__state time-waveform-workspace__state--error">
        <AlertCircle size={20} />
        <span>{error}</span>
      </div>
    )}

    <div className="time-waveform-workspace__plot-stage">
      {!error && activeSurface === 'waveform' && waveformYAxis && chartWidth > 0 && waveformSignals.length > 0 && (
        <WaveformChart
          signals={waveformSignals}
          width={chartWidth}
          yAxis={waveformYAxis}
        />
      )}

      {!error &&
        activeSurface === 'spectrum' &&
        spectrumXAxis &&
        spectrumYAxis &&
        Array.isArray(spectrumXAxis.ticks) &&
        Array.isArray(spectrumYAxis.ticks) &&
        chartWidth > 0 &&
        spectrumSignals.length > 0 && (
          <SpectrumChart
            signals={spectrumSignals}
            width={chartWidth}
            xAxis={spectrumXAxis}
            yAxis={spectrumYAxis}
          />
        )}
    </div>
  </div>
)

export { AnalysisWorkspaceChart }
