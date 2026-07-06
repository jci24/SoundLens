import { useRef } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { SpectrumChart } from '../../spectrum/components/SpectrumChart'
import { WaveformChart } from '../../waveform/components/WaveformChart'
import { useMeasuredChartWidth } from '../hooks/useMeasuredChartWidth'
import { useAnalysisWorkspacePanelCharts } from '../hooks/useAnalysisWorkspacePanelCharts'
import type { IAnalysisWorkspacePanel } from '../hooks/useAnalysisWorkspacePanels'
import type { IAnalysisRegionOfInterest, TSignalChartMode, IFrequencySpectrumAxis, IFrequencySpectrumSignal, ITimeWaveformSignal, ITimeWaveformResponse } from '../../types'
import './AnalysisWorkspacePanel.scss'

interface IAnalysisWorkspacePanelProps {
  chartWidth: number
  isCompareMode: boolean
  onRegionOfInterestChange: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  panel: IAnalysisWorkspacePanel
  regionOfInterest: IAnalysisRegionOfInterest | null
  signalChartMode: TSignalChartMode
  spectrumSignals: IFrequencySpectrumSignal[]
  spectrumXAxis: IFrequencySpectrumAxis | null
  spectrumYAxis: IFrequencySpectrumAxis | null
  waveformSignals: ITimeWaveformSignal[]
  waveformYAxis: ITimeWaveformResponse['yAxis'] | null
}

interface IAnalysisWorkspacePanelChartFrameProps {
  chartWidth: number
  chartItem: ReturnType<typeof useAnalysisWorkspacePanelCharts>[number]
  onRegionOfInterestChange: (regionOfInterest: IAnalysisRegionOfInterest | null) => void
  panelSurface: IAnalysisWorkspacePanel['surface']
  regionOfInterest: IAnalysisRegionOfInterest | null
  spectrumXAxis: IFrequencySpectrumAxis | null
  spectrumYAxis: IFrequencySpectrumAxis | null
  waveformYAxis: ITimeWaveformResponse['yAxis'] | null
}

const AnalysisWorkspacePanelChartFrame = ({
  chartWidth,
  chartItem,
  onRegionOfInterestChange,
  panelSurface,
  regionOfInterest,
  spectrumXAxis,
  spectrumYAxis,
  waveformYAxis,
}: IAnalysisWorkspacePanelChartFrameProps) => {
  const frameRef = useRef<HTMLDivElement | null>(null)
  const frameWidth = useMeasuredChartWidth(frameRef)
  const renderedChartWidth = frameWidth > 0 ? frameWidth : chartWidth

  return (
    <div className="time-waveform-workspace__panel-chart-frame" ref={frameRef}>
      {panelSurface === 'waveform' &&
        waveformYAxis &&
        renderedChartWidth > 0 &&
        chartItem.waveformSignals.length > 0 && (
          <WaveformChart
            onRegionOfInterestChange={onRegionOfInterestChange}
            regionOfInterest={regionOfInterest}
            signals={chartItem.waveformSignals}
            width={renderedChartWidth}
            yAxis={waveformYAxis}
          />
        )}

      {panelSurface === 'spectrum' &&
        spectrumXAxis &&
        spectrumYAxis &&
        Array.isArray(spectrumXAxis.ticks) &&
        Array.isArray(spectrumYAxis.ticks) &&
        renderedChartWidth > 0 &&
        chartItem.spectrumSignals.length > 0 && (
          <SpectrumChart
            signals={chartItem.spectrumSignals}
            width={renderedChartWidth}
            xAxis={spectrumXAxis}
            yAxis={spectrumYAxis}
          />
        )}
    </div>
  )
}

const AnalysisWorkspacePanel = ({
  chartWidth,
  isCompareMode,
  onRegionOfInterestChange,
  panel,
  regionOfInterest,
  signalChartMode,
  spectrumSignals,
  spectrumXAxis,
  spectrumYAxis,
  waveformSignals,
  waveformYAxis,
}: IAnalysisWorkspacePanelProps) => {
  const chartItems = useAnalysisWorkspacePanelCharts({
    panel,
    signalChartMode,
    spectrumSignals,
    waveformSignals,
  })

  return (
    <section className="time-waveform-workspace__panel-shell" aria-label={`${panel.title} panel`}>
      <header className="time-waveform-workspace__panel-header">
        <h3 className="time-waveform-workspace__panel-title">{panel.title}</h3>

        {panel.isRefreshing && !panel.isInitialLoading && (
          <div className="time-waveform-workspace__loading-pill" aria-live="polite">
            <Loader2 className="time-waveform-workspace__spinner" size={14} />
            <span>{panel.refreshingLabel}</span>
          </div>
        )}
      </header>

      {panel.isInitialLoading && (
        <div className="time-waveform-workspace__panel-state">
          <Loader2 className="time-waveform-workspace__spinner" size={20} />
          <span>{panel.loadingLabel}</span>
        </div>
      )}

      {panel.error && (
        <div className="time-waveform-workspace__panel-state time-waveform-workspace__panel-state--error">
          <AlertCircle size={20} />
          <span>{panel.error}</span>
        </div>
      )}

      {!panel.error && (
        <div
          className={`time-waveform-workspace__panel-chart-grid${signalChartMode === 'split' && chartItems.length > 1 ? ' time-waveform-workspace__panel-chart-grid--split' : ''}${isCompareMode ? ' time-waveform-workspace__panel-chart-grid--compare' : ''}`}
        >
          {chartItems.map((chartItem) => (
            <article className="time-waveform-workspace__panel-chart-card" key={chartItem.chartKey}>
              {chartItem.title && (
                <header className="time-waveform-workspace__panel-chart-card-header">
                  <span className="time-waveform-workspace__panel-chart-card-title">{chartItem.title}</span>
                </header>
              )}

              <AnalysisWorkspacePanelChartFrame
                chartItem={chartItem}
                chartWidth={chartWidth}
                onRegionOfInterestChange={onRegionOfInterestChange}
                panelSurface={panel.surface}
                regionOfInterest={regionOfInterest}
                spectrumXAxis={spectrumXAxis}
                spectrumYAxis={spectrumYAxis}
                waveformYAxis={waveformYAxis}
              />
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export { AnalysisWorkspacePanel }
