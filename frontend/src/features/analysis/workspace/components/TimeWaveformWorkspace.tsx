import { AnalysisWorkspaceChart } from './AnalysisWorkspaceChart'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'
import { RecordingRail } from '../../recording-rail/components/RecordingRail'
import { CopilotPanel } from '../../copilot/components/CopilotPanel'
import { useAnalysisWorkspaceMetrics } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import { useAnalysisWorkspacePanels } from '../hooks/useAnalysisWorkspacePanels'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import './TimeWaveformWorkspace.scss'

interface ITimeWaveformWorkspaceProps {
  importedFiles: IImportedFileSummary[]
}

const TimeWaveformWorkspace = ({ importedFiles }: ITimeWaveformWorkspaceProps) => {
  const {
    activeSurface,
    chartRef,
    chartWidth,
    expandedRecordings,
    isSpectrumInitialLoading,
    isSpectrumRefreshing,
    isWaveformInitialLoading,
    isWaveformRefreshing,
    layoutMode,
    spectrumFftSizeOptions,
    spectrumMaximumHz,
    spectrumRangeEndHz,
    spectrumRangeStartHz,
    spectrumViewport,
    selectedSpectrumPreset,
    spectrum,
    spectrumXAxis,
    spectrumSignals,
    waveformSignals,
    waveformError,
    recordings,
    selectedSignalIds,
    signalChartMode,
    showSpectrumPanel,
    showWaveformPanel,
    spectrumError,
    waveforms,
    onLayoutModeChange,
    onRecordingToggle,
    onSignalSelection,
    onSignalChartModeChange,
    onSpectrumPresetChange,
    onSpectrumRangeEndChange,
    onSpectrumRangeReset,
    onSpectrumRangeStartChange,
    onSurfaceChange,
    onRegionOfInterestChange,
    regionOfInterest,
  } = useTimeWaveformWorkspace(importedFiles)
  const waveformYAxis = waveforms?.yAxis ?? null
  const spectrumYAxis = spectrum?.yAxis ?? null
  const {
    hasActiveChart,
    panels,
  } = useAnalysisWorkspacePanels({
    chartWidth,
    isSpectrumInitialLoading,
    isSpectrumRefreshing,
    isWaveformInitialLoading,
    isWaveformRefreshing,
    showSpectrumPanel,
    showWaveformPanel,
    spectrumError,
    spectrumSignals,
    spectrumXAxis,
    spectrumYAxis,
    waveformError,
    waveformSignals,
    waveformYAxis,
  })
  const { hasMetricsPending, metricSignals } = useAnalysisWorkspaceMetrics({
    preferSpectrumMetrics: regionOfInterest !== null,
    spectrumSignals,
    waveformSignals,
  })

  return (
    <section
      className={`time-waveform-workspace${hasActiveChart ? ' time-waveform-workspace--revealed' : ''}${layoutMode === 'compare' ? ' time-waveform-workspace--compare' : ''}`}
      aria-label="Analysis workspace"
    >
      <AnalysisWorkspaceHeader
        activeSurface={activeSurface}
        layoutMode={layoutMode}
        onLayoutModeChange={onLayoutModeChange}
        onSignalChartModeChange={onSignalChartModeChange}
        onSpectrumPresetChange={onSpectrumPresetChange}
        onSpectrumRangeEndChange={onSpectrumRangeEndChange}
        onSpectrumRangeReset={onSpectrumRangeReset}
        onSpectrumRangeStartChange={onSpectrumRangeStartChange}
        onSurfaceChange={onSurfaceChange}
        selectedSpectrumPreset={selectedSpectrumPreset}
        selectedSignalCount={selectedSignalIds.length}
        signalChartMode={signalChartMode}
        showSpectrumPanel={showSpectrumPanel}
        spectrumFftSizeOptions={spectrumFftSizeOptions}
        spectrumMaximumHz={spectrumMaximumHz}
        spectrumRangeEndHz={spectrumRangeEndHz}
        spectrumRangeStartHz={spectrumRangeStartHz}
        spectrumViewport={spectrumViewport}
      />

      <div className="time-waveform-workspace__body">
        <RecordingRail
          expandedRecordings={expandedRecordings}
          onRecordingToggle={onRecordingToggle}
          onSignalSelection={onSignalSelection}
          recordings={recordings}
          selectedSignalIds={selectedSignalIds}
        />
        <div className="time-waveform-workspace__main-pane">
          {activeSurface === 'copilot' ? (
            <CopilotPanel
              regionOfInterest={regionOfInterest}
              selectedSignalIds={selectedSignalIds}
            />
          ) : (
            <>
              {regionOfInterest && (
                <section className="time-waveform-workspace__roi-summary" aria-label="Selected time region">
                  <div className="time-waveform-workspace__roi-copy">
                    <span className="time-waveform-workspace__roi-title">Selected region</span>
                    <span className="time-waveform-workspace__roi-values">
                      {`${formatCompactDuration(regionOfInterest.startTimeSeconds)} to ${formatCompactDuration(regionOfInterest.endTimeSeconds)} · ${formatCompactDuration(regionOfInterest.durationSeconds)}`}
                    </span>
                  </div>
                  <button
                    className="time-waveform-workspace__roi-clear"
                    type="button"
                    onClick={() => onRegionOfInterestChange(null)}
                  >
                    Clear region
                  </button>
                </section>
              )}
              <AnalysisWorkspaceChart
                chartRef={chartRef}
                chartWidth={chartWidth}
                hasMetricsPending={hasMetricsPending}
                isCompareMode={layoutMode === 'compare'}
                metricSignals={metricSignals}
                onRegionOfInterestChange={onRegionOfInterestChange}
                panels={panels}
                regionOfInterest={regionOfInterest}
                signalChartMode={signalChartMode}
                spectrumSignals={spectrumSignals}
                spectrumXAxis={spectrumXAxis}
                spectrumYAxis={spectrumYAxis}
                waveformSignals={waveformSignals}
                waveformYAxis={waveformYAxis}
              />
            </>
          )}
        </div>
      </div>
    </section>
  )
}

export { TimeWaveformWorkspace }
