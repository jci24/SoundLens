import { AlertCircle, Loader2 } from 'lucide-react'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'
import { RecordingRail } from './RecordingRail'
import { SpectrumChart } from './SpectrumChart'
import { WaveformChart } from './WaveformChart'
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

  const waveformYAxis = waveforms?.yAxis ?? null
  const spectrumYAxis = spectrum?.yAxis ?? null
  const hasActiveChart =
    activeSurface === 'waveform'
      ? waveformSignals.length > 0 && waveformYAxis !== null && chartWidth > 0
      : spectrumSignals.length > 0 && spectrumXAxis !== null && spectrumYAxis !== null && chartWidth > 0

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

        <div className="time-waveform-workspace__chart-shell" ref={chartRef}>
          {isInitialLoading && (
            <div className="time-waveform-workspace__state">
              <Loader2 className="time-waveform-workspace__spinner" size={20} />
              <span>{activeSurface === 'waveform' ? 'Generating waveform bins' : 'Generating spectrum bins'}</span>
            </div>
          )}

          {isRefreshing && !isInitialLoading && (
            <div className="time-waveform-workspace__loading-pill" aria-live="polite">
              <Loader2 className="time-waveform-workspace__spinner" size={14} />
              <span>{activeSurface === 'waveform' ? 'Updating overlay' : 'Updating spectrum'}</span>
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
      </div>
    </section>
  )
}

export { TimeWaveformWorkspace }
