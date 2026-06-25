import type { RefObject } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { SpectrumChart } from './SpectrumChart'
import { WaveformChart } from './WaveformChart'
import type { IFrequencySpectrumAxis, IFrequencySpectrumSignal, ITimeWaveformSignal, ITimeWaveformResponse } from '../types'
import type { TAnalysisSurface } from '../hooks/useTimeWaveformWorkspace'

interface IAnalysisWorkspaceChartProps {
  activeSurface: TAnalysisSurface
  chartRef: RefObject<HTMLDivElement | null>
  chartWidth: number
  error: string | null
  isInitialLoading: boolean
  isRefreshing: boolean
  loadingLabel: string
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
  isRefreshing,
  loadingLabel,
  refreshingLabel,
  spectrumSignals,
  spectrumXAxis,
  spectrumYAxis,
  waveformSignals,
  waveformYAxis,
}: IAnalysisWorkspaceChartProps) => (
  <div className="time-waveform-workspace__chart-shell" ref={chartRef}>
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
)

export { AnalysisWorkspaceChart }
