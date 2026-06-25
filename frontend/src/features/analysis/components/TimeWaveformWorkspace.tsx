import { AnalysisWorkspaceChart } from './AnalysisWorkspaceChart'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'
import { RecordingRail } from './RecordingRail'
import { useAnalysisWorkspaceChartState } from '../hooks/useAnalysisWorkspaceChartState'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import type { IImportedFileSummary } from '../../../common/contracts/import'
import './TimeWaveformWorkspace.scss'

interface ITimeWaveformWorkspaceProps {
  importedFiles: IImportedFileSummary[]
}

const TimeWaveformWorkspace = ({ importedFiles }: ITimeWaveformWorkspaceProps) => {
  const {
    activeSurface,
    chartRef,
    chartWidth,
    error,
    expandedRecordings,
    isInitialLoading,
    isRefreshing,
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
    recordings,
    selectedSignalIds,
    waveforms,
    onRecordingToggle,
    onSignalSelection,
    onSpectrumPresetChange,
    onSpectrumRangeEndChange,
    onSpectrumRangeReset,
    onSpectrumRangeStartChange,
    onSurfaceChange,
  } = useTimeWaveformWorkspace(importedFiles)
  const {
    hasActiveChart,
    loadingLabel,
    refreshingLabel,
    spectrumYAxis,
    waveformYAxis,
  } = useAnalysisWorkspaceChartState({
    activeSurface,
    chartWidth,
    spectrum,
    spectrumSignals,
    spectrumXAxis,
    waveforms,
    waveformSignals,
  })

  return (
    <section
      className={`time-waveform-workspace${hasActiveChart ? ' time-waveform-workspace--revealed' : ''}`}
      aria-label="Analysis workspace"
    >
      <AnalysisWorkspaceHeader
        activeSurface={activeSurface}
        onSpectrumPresetChange={onSpectrumPresetChange}
        onSpectrumRangeEndChange={onSpectrumRangeEndChange}
        onSpectrumRangeReset={onSpectrumRangeReset}
        onSpectrumRangeStartChange={onSpectrumRangeStartChange}
        onSurfaceChange={onSurfaceChange}
        selectedSpectrumPreset={selectedSpectrumPreset}
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
        <AnalysisWorkspaceChart
          activeSurface={activeSurface}
          chartRef={chartRef}
          chartWidth={chartWidth}
          error={error}
          isInitialLoading={isInitialLoading}
          isRefreshing={isRefreshing}
          loadingLabel={loadingLabel}
          refreshingLabel={refreshingLabel}
          spectrumSignals={spectrumSignals}
          spectrumXAxis={spectrumXAxis}
          spectrumYAxis={spectrumYAxis}
          waveformSignals={waveformSignals}
          waveformYAxis={waveformYAxis}
        />
      </div>
    </section>
  )
}

export { TimeWaveformWorkspace }
