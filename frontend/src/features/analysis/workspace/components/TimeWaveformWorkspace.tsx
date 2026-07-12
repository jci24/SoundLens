import { AnalysisWorkspaceChart } from './AnalysisWorkspaceChart'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'
import { RecordingRail } from '../../recording-rail/components/RecordingRail'
import { useAnalysisWorkspaceMetrics } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import { useAnalysisWorkspacePanels } from '../hooks/useAnalysisWorkspacePanels'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import { exportReportMarkdown } from '../../report/services/exportReportMarkdown'
import { downloadTextFile } from '../../report/utils/reportDownload'
import { getComparisonSetupSummary } from '../../utils/analysisWorkspaceState'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import { useState } from 'react'
import { toast } from 'sonner'
import './TimeWaveformWorkspace.scss'

interface ITimeWaveformWorkspaceProps {
  importedFiles: IImportedFileSummary[]
  isCopilotOpen: boolean
  onCopilotToggle: () => void
}

const TimeWaveformWorkspace = ({ importedFiles, isCopilotOpen, onCopilotToggle }: ITimeWaveformWorkspaceProps) => {
  const [isExporting, setIsExporting] = useState(false)
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
    recordingGroupAssignments,
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
    onRecordingGroupAssignment,
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
  const comparisonSetup = getComparisonSetupSummary(recordings, recordingGroupAssignments)
  const comparisonGuidance =
    comparisonSetup.state === 'valid'
      ? {
          label: 'Ready',
          copy: 'Both groups are populated. Compare mode is ready.',
        }
      : comparisonSetup.state === 'incomplete'
        ? {
            label: 'Incomplete',
            copy: 'Assign at least one recording to the empty group to unlock compare mode.',
          }
        : {
            label: 'Not ready',
            copy: 'Assign recordings to Group A and Group B to begin a valid comparison.',
          }

  const handleExportReport = async () => {
    try {
      setIsExporting(true)

      const response = await exportReportMarkdown({
        activeSurface,
        layoutMode,
        signalChartMode,
        recordings: recordings.map((recording) => ({
          recordingId: recording.recordingId,
          fileName: recording.fileName,
          sizeBytes: recording.sizeBytes,
          durationSeconds: recording.durationSeconds,
          sampleRate: recording.sampleRate,
          channels: recording.channels,
          channelMode: recording.channelMode,
          signals: recording.signals.map((signal) => ({
            signalId: signal.signalId,
            channelIndex: signal.channelIndex,
            displayName: signal.displayName,
            fileName: recording.fileName,
          })),
        })),
        selectedSignalEvidence: metricSignals.map((signal) => ({
          signalId: signal.signalId,
          fileName: signal.recordingFileName,
          displayName: signal.displayName,
          durationSeconds: signal.durationSeconds,
          sampleRate: signal.sampleRate,
          metrics: {
            peakAmplitude: signal.peakAmplitude,
            rmsAmplitude: signal.rmsAmplitude,
            crestFactor: signal.crestFactor,
            clippingSampleCount: signal.clippingSampleCount,
            hasClipping: signal.hasClipping,
          },
          findings: signal.findings,
        })),
        selectedSignalIds: selectedSignalIds.length > 0 ? selectedSignalIds : undefined,
        startTimeSeconds: regionOfInterest?.startTimeSeconds,
        endTimeSeconds: regionOfInterest?.endTimeSeconds,
      })

      downloadTextFile(response.fileName, response.markdown)
      toast.success(`Downloaded ${response.fileName}.`)
    } catch {
      toast.error('The markdown report could not be prepared.')
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <section
      className={`time-waveform-workspace${hasActiveChart ? ' time-waveform-workspace--revealed' : ''}${layoutMode === 'compare' ? ' time-waveform-workspace--compare' : ''}`}
      aria-label="Analysis workspace"
    >
      <AnalysisWorkspaceHeader
        activeSurface={activeSurface}
        canEnterCompareMode={comparisonSetup.state === 'valid'}
        isCopilotOpen={isCopilotOpen}
        isExporting={isExporting}
        layoutMode={layoutMode}
        onCopilotToggle={onCopilotToggle}
        onExportReport={handleExportReport}
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
          onRecordingGroupAssignment={onRecordingGroupAssignment}
          onRecordingToggle={onRecordingToggle}
          onSignalSelection={onSignalSelection}
          recordings={recordings}
          recordingGroupAssignments={recordingGroupAssignments}
          selectedSignalIds={selectedSignalIds}
        />
        <div className="time-waveform-workspace__main-pane">
          <section
            className={`time-waveform-workspace__comparison-scope time-waveform-workspace__comparison-scope--${comparisonSetup.state}`}
            aria-label="Comparison scope"
          >
            <div className="time-waveform-workspace__comparison-scope-meta">
              <span className="time-waveform-workspace__comparison-scope-kicker">Setup</span>
              <span className="time-waveform-workspace__comparison-scope-title">Comparison scope</span>
            </div>
            <div className="time-waveform-workspace__comparison-scope-metrics">
              <span className="time-waveform-workspace__comparison-scope-pill time-waveform-workspace__comparison-scope-pill--A">
                A <strong>{comparisonSetup.counts.A}</strong>
              </span>
              <span className="time-waveform-workspace__comparison-scope-pill time-waveform-workspace__comparison-scope-pill--B">
                B <strong>{comparisonSetup.counts.B}</strong>
              </span>
              <span className="time-waveform-workspace__comparison-scope-pill time-waveform-workspace__comparison-scope-pill--unassigned">
                Unassigned <strong>{comparisonSetup.counts.unassigned}</strong>
              </span>
            </div>
          </section>
          <section
            className={`time-waveform-workspace__comparison-guidance time-waveform-workspace__comparison-guidance--${comparisonSetup.state}`}
            aria-label="Comparison setup guidance"
          >
            <span className="time-waveform-workspace__comparison-guidance-label">
              {comparisonGuidance.label}
            </span>
            <p className="time-waveform-workspace__comparison-guidance-copy">{comparisonGuidance.copy}</p>
          </section>
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
        </div>
      </div>
    </section>
  )
}

export { TimeWaveformWorkspace }
