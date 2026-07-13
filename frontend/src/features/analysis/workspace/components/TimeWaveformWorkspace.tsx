import { AnalysisWorkspaceChart } from './AnalysisWorkspaceChart'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'
import { RecordingRail } from '../../recording-rail/components/RecordingRail'
import { useAnalysisWorkspaceMetrics } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import { useAnalysisWorkspacePanels } from '../hooks/useAnalysisWorkspacePanels'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import { exportReportMarkdown } from '../../report/services/exportReportMarkdown'
import { getRecordingComparison } from '../../services/recordingComparison'
import { downloadTextFile } from '../../report/utils/reportDownload'
import { getComparisonSetupSummary } from '../../utils/analysisWorkspaceState'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import type {
  IRecordingComparisonMetricAggregate,
  IRecordingComparisonResponse,
  IRecordingComparisonSignalObservation,
} from '../../types'
import { useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import './TimeWaveformWorkspace.scss'

interface ITimeWaveformWorkspaceProps {
  importedFiles: IImportedFileSummary[]
  isCopilotOpen: boolean
  onCopilotToggle: () => void
}

const TimeWaveformWorkspace = ({ importedFiles, isCopilotOpen, onCopilotToggle }: ITimeWaveformWorkspaceProps) => {
  const [isExporting, setIsExporting] = useState(false)
  const [comparisonResults, setComparisonResults] = useState<IRecordingComparisonResponse | null>(null)
  const [comparisonError, setComparisonError] = useState<string | null>(null)
  const [isComparisonLoading, setIsComparisonLoading] = useState(false)
  const [selectedMetricKey, setSelectedMetricKey] =
    useState<IRecordingComparisonMetricAggregate['metricKey'] | null>(null)
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
  const groupARecordings = useMemo(
    () =>
      recordings.filter((recording) => (recordingGroupAssignments[recording.recordingId] ?? 'unassigned') === 'A'),
    [recordingGroupAssignments, recordings]
  )
  const groupBRecordings = useMemo(
    () =>
      recordings.filter((recording) => (recordingGroupAssignments[recording.recordingId] ?? 'unassigned') === 'B'),
    [recordingGroupAssignments, recordings]
  )
  const canRequestPairwiseComparison =
    layoutMode === 'compare' && comparisonSetup.state === 'valid' && groupARecordings.length === 1 && groupBRecordings.length === 1
  const needsPairwiseReduction =
    layoutMode === 'compare' && comparisonSetup.state === 'valid' && !canRequestPairwiseComparison
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
  const rankedMetrics = useMemo(
    () =>
      [...(comparisonResults?.aggregateMetrics ?? [])].sort(
        (left, right) => Math.abs(right.meanDifference) - Math.abs(left.meanDifference)
      ),
    [comparisonResults?.aggregateMetrics]
  )
  const activeMetric = useMemo(
    () =>
      rankedMetrics.find((metric) => metric.metricKey === selectedMetricKey) ?? rankedMetrics[0] ?? null,
    [rankedMetrics, selectedMetricKey]
  )
  const activeObservation = useMemo(() => {
    if (!comparisonResults || !activeMetric) {
      return null
    }

    return [...comparisonResults.signalObservations].sort(
      (left, right) =>
        Math.abs(getObservationDelta(right, activeMetric.metricKey)) -
        Math.abs(getObservationDelta(left, activeMetric.metricKey))
    )[0] ?? null
  }, [activeMetric, comparisonResults])

  useEffect(() => {
    if (!canRequestPairwiseComparison) {
      setComparisonResults(null)
      setComparisonError(null)
      setIsComparisonLoading(false)
      setSelectedMetricKey(null)
      return
    }

    let isCurrent = true

    setIsComparisonLoading(true)

    void getRecordingComparison(
      groupARecordings[0].recordingId,
      groupBRecordings[0].recordingId,
      regionOfInterest
        ? {
            startTimeSeconds: regionOfInterest.startTimeSeconds,
            endTimeSeconds: regionOfInterest.endTimeSeconds,
          }
        : null
    )
      .then((response) => {
        if (!isCurrent) {
          return
        }

        setComparisonResults(response)
        setComparisonError(null)
        setSelectedMetricKey(null)
      })
      .catch((caughtError) => {
        if (!isCurrent) {
          return
        }

        setComparisonResults(null)
        setComparisonError(
          caughtError instanceof Error ? caughtError.message : 'Comparison results could not be prepared.'
        )
      })
      .finally(() => {
        if (!isCurrent) {
          return
        }

        setIsComparisonLoading(false)
      })

    return () => {
      isCurrent = false
    }
  }, [canRequestPairwiseComparison, groupARecordings, groupBRecordings, regionOfInterest])

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
            <p className="time-waveform-workspace__comparison-guidance-copy">
              {needsPairwiseReduction
                ? 'Ranked differences currently support one recording in Group A and one in Group B. Reduce each group to one recording to review deterministic deltas.'
                : comparisonGuidance.copy}
            </p>
          </section>
          {layoutMode === 'compare' && (
            <section className="time-waveform-workspace__comparison-results" aria-label="Ranked comparison results">
              <div className="time-waveform-workspace__comparison-results-header">
                <div>
                  <span className="time-waveform-workspace__comparison-results-kicker">Results</span>
                  <h3 className="time-waveform-workspace__comparison-results-title">Ranked differences</h3>
                </div>
                {comparisonResults && (
                  <span className="time-waveform-workspace__comparison-results-summary">
                    {comparisonResults.recordingA.fileName} vs {comparisonResults.recordingB.fileName}
                  </span>
                )}
              </div>

              {needsPairwiseReduction && (
                <p className="time-waveform-workspace__comparison-results-empty">
                  Pairwise compare mode is active, but the current backend slice only ranks one recording from Group A against one recording from Group B.
                </p>
              )}

              {!needsPairwiseReduction && isComparisonLoading && (
                <p className="time-waveform-workspace__comparison-results-empty">
                  Preparing ranked differences from the current comparison pair.
                </p>
              )}

              {!needsPairwiseReduction && comparisonError && (
                <p className="time-waveform-workspace__comparison-results-error">{comparisonError}</p>
              )}

              {!needsPairwiseReduction && !isComparisonLoading && !comparisonError && comparisonResults && (
                <>
                  <div className="time-waveform-workspace__comparison-ranking">
                    {rankedMetrics.map((metric) => (
                      <button
                        key={metric.metricKey}
                        className={`time-waveform-workspace__comparison-ranking-card${activeMetric?.metricKey === metric.metricKey ? ' time-waveform-workspace__comparison-ranking-card--active' : ''}`}
                        type="button"
                        onClick={() => setSelectedMetricKey(metric.metricKey)}
                      >
                        <span className="time-waveform-workspace__comparison-ranking-label">
                          {formatComparisonMetricLabel(metric.metricKey)}
                        </span>
                        <strong className="time-waveform-workspace__comparison-ranking-value">
                          {formatAggregateValue(metric.meanDifference, metric.unit)}
                        </strong>
                        <span className="time-waveform-workspace__comparison-ranking-meta">
                          Spread {formatAggregateValue(metric.spread, metric.unit)} · Pairs {metric.comparedPairCount}
                        </span>
                      </button>
                    ))}
                  </div>

                  {activeMetric && activeObservation && (
                    <section className="time-waveform-workspace__comparison-focus" aria-label="Selected ranked difference">
                      <div>
                        <span className="time-waveform-workspace__comparison-focus-kicker">Evidence focus</span>
                        <h4 className="time-waveform-workspace__comparison-focus-title">
                          {formatComparisonMetricLabel(activeMetric.metricKey)}
                        </h4>
                      </div>
                      <div className="time-waveform-workspace__comparison-focus-grid">
                        <div className="time-waveform-workspace__comparison-focus-stat">
                          <span>Mean delta A-B</span>
                          <strong>{formatAggregateValue(activeMetric.meanDifference, activeMetric.unit)}</strong>
                        </div>
                        <div className="time-waveform-workspace__comparison-focus-stat">
                          <span>Median</span>
                          <strong>{formatAggregateValue(activeMetric.medianDifference, activeMetric.unit)}</strong>
                        </div>
                        <div className="time-waveform-workspace__comparison-focus-stat">
                          <span>Coverage</span>
                          <strong>
                            {activeMetric.comparedPairCount} pair{activeMetric.comparedPairCount === 1 ? '' : 's'}
                          </strong>
                        </div>
                        <div className="time-waveform-workspace__comparison-focus-stat">
                          <span>Missing</span>
                          <strong>{activeMetric.missingValueCount}</strong>
                        </div>
                      </div>
                      <p className="time-waveform-workspace__comparison-focus-copy">
                        Strongest aligned pair: {activeObservation.displayNameA} vs {activeObservation.displayNameB} ·
                        A {formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'A'), activeMetric.unit)} ·
                        B {formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'B'), activeMetric.unit)} ·
                        Delta {formatAggregateValue(getObservationDelta(activeObservation, activeMetric.metricKey), activeMetric.unit)}
                      </p>
                    </section>
                  )}

                  {comparisonResults.limitations.length > 0 && (
                    <section className="time-waveform-workspace__comparison-limitations" aria-label="Comparison limitations">
                      {comparisonResults.limitations.map((limitation) => (
                        <p key={`${limitation.code}-${limitation.detail}`}>
                          <strong>{formatLimitationLabel(limitation.code)}:</strong> {limitation.detail}
                        </p>
                      ))}
                    </section>
                  )}
                </>
              )}
            </section>
          )}
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

const formatComparisonMetricLabel = (metricKey: IRecordingComparisonMetricAggregate['metricKey']) => {
  switch (metricKey) {
    case 'peakAmplitudeDelta':
      return 'Peak amplitude'
    case 'rmsAmplitudeDelta':
      return 'RMS amplitude'
    case 'crestFactorDelta':
      return 'Crest factor'
    case 'clippingSampleCountDelta':
      return 'Clipping samples'
  }
}

const formatAggregateValue = (value: number, unit: string) => {
  if (unit === 'samples') {
    return `${value.toFixed(0)} ${unit}`
  }

  return `${value.toFixed(3)} ${unit}`
}

const getObservationDelta = (
  observation: IRecordingComparisonSignalObservation,
  metricKey: IRecordingComparisonMetricAggregate['metricKey']
) => {
  switch (metricKey) {
    case 'peakAmplitudeDelta':
      return observation.peakAmplitudeDelta
    case 'rmsAmplitudeDelta':
      return observation.rmsAmplitudeDelta
    case 'crestFactorDelta':
      return observation.crestFactorDelta
    case 'clippingSampleCountDelta':
      return observation.clippingSampleCountDelta
  }
}

const getObservationValue = (
  observation: IRecordingComparisonSignalObservation,
  metricKey: IRecordingComparisonMetricAggregate['metricKey'],
  side: 'A' | 'B'
) => {
  switch (metricKey) {
    case 'peakAmplitudeDelta':
      return side === 'A' ? observation.peakAmplitudeA : observation.peakAmplitudeB
    case 'rmsAmplitudeDelta':
      return side === 'A' ? observation.rmsAmplitudeA : observation.rmsAmplitudeB
    case 'crestFactorDelta':
      return side === 'A' ? observation.crestFactorA : observation.crestFactorB
    case 'clippingSampleCountDelta':
      return side === 'A' ? observation.clippingSampleCountA : observation.clippingSampleCountB
  }
}

const formatLimitationLabel = (code: string) => {
  switch (code) {
    case 'LowCoverage':
      return 'Low coverage'
    case 'Missing':
      return 'Missing match'
    case 'Ambiguous':
      return 'Ambiguous match'
    default:
      return code
  }
}
