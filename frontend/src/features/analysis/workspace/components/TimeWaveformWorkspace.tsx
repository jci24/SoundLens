import { AnalysisWorkspaceChart } from './AnalysisWorkspaceChart'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'
import { ComparisonReportDialog } from '../../report/components/ComparisonReportDialog'
import { RecordingRail } from '../../recording-rail/components/RecordingRail'
import { useAnalysisWorkspaceMetrics } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import { useReportExport } from '../../report/hooks/useReportExport'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import { useAnalysisWorkspacePanels } from '../hooks/useAnalysisWorkspacePanels'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import { getRecordingComparison } from '../../services/recordingComparison'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import { getComparisonSetupSummary } from '../../utils/analysisWorkspaceState'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import type {
  IComparisonCopilotSelection,
  IRecordingComparisonMetricAggregate,
  IRecordingComparisonResponse,
  IRecordingComparisonSignalObservation,
} from '../../types'
import { ChevronDown, ChevronUp } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import './TimeWaveformWorkspace.scss'

interface ITimeWaveformWorkspaceProps {
  importedFiles: IImportedFileSummary[]
  isCopilotOpen: boolean
  onCopilotToggle: () => void
}

interface IComparisonRequestState {
  error: string | null
  requestKey: string | null
  results: IRecordingComparisonResponse | null
}

const TimeWaveformWorkspace = ({ importedFiles, isCopilotOpen, onCopilotToggle }: ITimeWaveformWorkspaceProps) => {
  const setComparisonCopilotContext = useAnalysisWorkspaceStore((state) => state.setComparisonCopilotContext)
  const [comparisonRequestState, setComparisonRequestState] = useState<IComparisonRequestState>({
    error: null,
    requestKey: null,
    results: null,
  })
  const [isComparisonDetailsOpen, setIsComparisonDetailsOpen] = useState(false)
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
  const activePairRecordingA = groupARecordings[0] ?? null
  const activePairRecordingB = groupBRecordings[0] ?? null
  const queuedGroupARecordings = activePairRecordingA ? groupARecordings.slice(1) : groupARecordings
  const queuedGroupBRecordings = activePairRecordingB ? groupBRecordings.slice(1) : groupBRecordings
  const canRequestPairwiseComparison =
    layoutMode === 'compare' && comparisonSetup.state === 'valid' && groupARecordings.length === 1 && groupBRecordings.length === 1
  const needsPairwiseReduction =
    layoutMode === 'compare' && comparisonSetup.state === 'valid' && !canRequestPairwiseComparison
  const queuedComparisonCopy = useMemo(() => {
    const queuedSegments: string[] = []

    if (queuedGroupARecordings.length > 0) {
      queuedSegments.push(`A waiting: ${queuedGroupARecordings.map((recording) => recording.fileName).join(', ')}`)
    }

    if (queuedGroupBRecordings.length > 0) {
      queuedSegments.push(`B waiting: ${queuedGroupBRecordings.map((recording) => recording.fileName).join(', ')}`)
    }

    return queuedSegments.join(' · ')
  }, [queuedGroupARecordings, queuedGroupBRecordings])
  const activeComparisonRequestKey = canRequestPairwiseComparison
    ? [
        groupARecordings[0].recordingId,
        groupBRecordings[0].recordingId,
        regionOfInterest?.startTimeSeconds ?? 'full',
        regionOfInterest?.endTimeSeconds ?? 'full',
      ].join(':')
    : null
  const comparisonResults =
    activeComparisonRequestKey !== null && comparisonRequestState.requestKey === activeComparisonRequestKey
      ? comparisonRequestState.results
      : null
  const comparisonError =
    activeComparisonRequestKey !== null && comparisonRequestState.requestKey === activeComparisonRequestKey
      ? comparisonRequestState.error
      : null
  const isComparisonLoading =
    activeComparisonRequestKey !== null && comparisonRequestState.requestKey !== activeComparisonRequestKey
  const comparisonGuidance =
    canRequestPairwiseComparison
      ? {
          label: 'Ready',
          copy: 'Pair selected.',
        }
      : comparisonSetup.state === 'valid'
      ? {
          label: 'Ready',
          copy: `A ${groupARecordings.length} · B ${groupBRecordings.length}`,
        }
      : comparisonSetup.state === 'incomplete'
        ? {
            label: 'Incomplete',
            copy: 'Choose the empty target.',
          }
        : {
          label: 'Not ready',
          copy: 'Choose A and B.',
        }
  const pairwiseReductionMessage =
    activePairRecordingA && activePairRecordingB
      ? `This slice compares one pair at a time. Now using ${activePairRecordingA.fileName} vs ${activePairRecordingB.fileName}.${queuedComparisonCopy ? ` ${queuedComparisonCopy}.` : ''}`
      : 'This slice compares one pair at a time.'
  const comparisonMetrics = comparisonResults?.aggregateMetrics ?? []
  const activeMetric =
    comparisonMetrics.find((metric) => metric.metricKey === selectedMetricKey) ?? comparisonMetrics[0] ?? null
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
  const coverageSummary = useMemo(
    () => getComparisonCoverageSummary(comparisonResults, activeMetric),
    [activeMetric, comparisonResults]
  )
  const comparisonCopilotContext = useMemo<IComparisonCopilotSelection | null>(() => {
    if (
      layoutMode !== 'compare' ||
      !comparisonResults ||
      !activeMetric ||
      !activeObservation ||
      !activePairRecordingA ||
      !activePairRecordingB
    ) {
      return null
    }

    return {
      recordingIdA: activePairRecordingA.recordingId,
      recordingIdB: activePairRecordingB.recordingId,
      metricKey: activeMetric.metricKey,
      signalIdA: activeObservation.signalIdA,
      signalIdB: activeObservation.signalIdB,
    }
  }, [
    activeMetric,
    activeObservation,
    activePairRecordingA,
    activePairRecordingB,
    comparisonResults,
    layoutMode,
  ])
  const roiScopeLabel = regionOfInterest
    ? `${formatCompactDuration(regionOfInterest.startTimeSeconds)} to ${formatCompactDuration(regionOfInterest.endTimeSeconds)} · ${formatCompactDuration(regionOfInterest.durationSeconds)}`
    : null
  const {
    canExportReport,
    comparisonReportTitle,
    excludedRecordings,
    handleComparisonReportExport,
    handleExportReport,
    isComparisonReportOpen,
    isExporting,
    setComparisonReportTitle,
    setIsComparisonReportOpen,
  } = useReportExport({
    activePairRecordingA,
    activePairRecordingB,
    activeSurface,
    comparisonSelection: comparisonCopilotContext,
    layoutMode,
    metricSignals,
    recordingGroupAssignments,
    recordings,
    regionOfInterest,
    selectedSignalIds,
    signalChartMode,
  })

  useEffect(() => {
    if (!canRequestPairwiseComparison) {
      return
    }

    let isCurrent = true
    const requestKey = [
      groupARecordings[0].recordingId,
      groupBRecordings[0].recordingId,
      regionOfInterest?.startTimeSeconds ?? 'full',
      regionOfInterest?.endTimeSeconds ?? 'full',
    ].join(':')

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

        setComparisonRequestState({
          error: null,
          requestKey,
          results: response,
        })
        setIsComparisonDetailsOpen(false)
      })
      .catch((caughtError) => {
        if (!isCurrent) {
          return
        }

        setComparisonRequestState({
          error: caughtError instanceof Error ? caughtError.message : 'Comparison results could not be prepared.',
          requestKey,
          results: null,
        })
      })

    return () => {
      isCurrent = false
    }
  }, [canRequestPairwiseComparison, groupARecordings, groupBRecordings, regionOfInterest])

  useEffect(() => {
    setComparisonCopilotContext(comparisonCopilotContext)

    return () => {
      setComparisonCopilotContext(null)
    }
  }, [comparisonCopilotContext, setComparisonCopilotContext])

  return (
    <section
      className={`time-waveform-workspace${hasActiveChart ? ' time-waveform-workspace--revealed' : ''}${layoutMode === 'compare' ? ' time-waveform-workspace--compare' : ''}`}
      aria-label="Analysis workspace"
    >
      <AnalysisWorkspaceHeader
        activeSurface={activeSurface}
        canEnterCompareMode={comparisonSetup.state === 'valid'}
        canExportReport={canExportReport}
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

      {activePairRecordingA && activePairRecordingB && (
        <ComparisonReportDialog
          excludedRecordings={excludedRecordings}
          fileNameA={activePairRecordingA.fileName}
          fileNameB={activePairRecordingB.fileName}
          isExporting={isExporting}
          isOpen={isComparisonReportOpen}
          onExport={handleComparisonReportExport}
          onOpenChange={setIsComparisonReportOpen}
          onTitleChange={setComparisonReportTitle}
          regionOfInterest={regionOfInterest}
          title={comparisonReportTitle}
        />
      )}

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
            className={`time-waveform-workspace__comparison-guidance time-waveform-workspace__comparison-guidance--${comparisonSetup.state}`}
            aria-label="Comparison setup guidance"
          >
            <div className="time-waveform-workspace__comparison-guidance-main">
              <span className="time-waveform-workspace__comparison-guidance-label">
                {comparisonGuidance.label}
              </span>
              <p className="time-waveform-workspace__comparison-guidance-copy">
                {needsPairwiseReduction ? 'Using the first recording on each side.' : comparisonGuidance.copy}
              </p>
            </div>
            {comparisonSetup.state === 'valid' && (
              <div className="time-waveform-workspace__comparison-guidance-context">
                <div className="time-waveform-workspace__comparison-guidance-pair" aria-label="Active comparison pair">
                  <span className="time-waveform-workspace__comparison-guidance-pair-pill time-waveform-workspace__comparison-guidance-pair-pill--A">
                    <strong>Compare A</strong>
                    <span>{activePairRecordingA ? activePairRecordingA.fileName : `${groupARecordings.length} recordings`}</span>
                  </span>
                  <span className="time-waveform-workspace__comparison-guidance-pair-divider">vs</span>
                  <span className="time-waveform-workspace__comparison-guidance-pair-pill time-waveform-workspace__comparison-guidance-pair-pill--B">
                    <strong>Compare B</strong>
                    <span>{activePairRecordingB ? activePairRecordingB.fileName : `${groupBRecordings.length} recordings`}</span>
                  </span>
                </div>
                {needsPairwiseReduction && queuedComparisonCopy && (
                  <p className="time-waveform-workspace__comparison-guidance-queue" aria-label="Queued comparison recordings">
                    {queuedComparisonCopy}
                  </p>
                )}
                {layoutMode === 'compare' && regionOfInterest && roiScopeLabel && (
                  <div className="time-waveform-workspace__comparison-guidance-scope" aria-label="Comparison scope">
                    <span className="time-waveform-workspace__comparison-guidance-scope-kicker">ROI</span>
                    <span className="time-waveform-workspace__comparison-guidance-scope-values">{roiScopeLabel}</span>
                    <button
                      aria-label="Clear selected comparison region"
                      className="time-waveform-workspace__comparison-guidance-scope-clear"
                      type="button"
                      onClick={() => onRegionOfInterestChange(null)}
                    >
                      Clear
                    </button>
                  </div>
                )}
              </div>
            )}
          </section>
          {layoutMode === 'compare' && (
            <section className="time-waveform-workspace__comparison-results" aria-label="Comparison metrics">
              <div className="time-waveform-workspace__comparison-results-header">
                <div>
                  <h3 className="time-waveform-workspace__comparison-results-title">Comparison metrics</h3>
                </div>
                {comparisonResults && (
                  <div className="time-waveform-workspace__comparison-results-summary-group">
                    <span
                      className={`time-waveform-workspace__comparison-results-badge time-waveform-workspace__comparison-results-badge--${coverageSummary.tone}`}
                    >
                      {coverageSummary.label}
                    </span>
                    {(activeMetric || coverageSummary.limitationCount > 0) && (
                      <>
                        <span className="time-waveform-workspace__comparison-results-summary">
                          {coverageSummary.limitationCount} limitation{coverageSummary.limitationCount === 1 ? '' : 's'}
                        </span>
                        {!isComparisonDetailsOpen && (
                          <button
                            aria-controls="comparison-metric-evidence"
                            aria-expanded="false"
                            className="time-waveform-workspace__comparison-results-details-toggle"
                            type="button"
                            onClick={() => setIsComparisonDetailsOpen(true)}
                          >
                            Evidence &amp; limitations
                            <ChevronDown aria-hidden="true" size={14} />
                          </button>
                        )}
                      </>
                    )}
                  </div>
                )}
              </div>

              {needsPairwiseReduction && (
                <p className="time-waveform-workspace__comparison-results-empty">
                  {pairwiseReductionMessage}
                </p>
              )}

              {!needsPairwiseReduction && isComparisonLoading && (
                <p className="time-waveform-workspace__comparison-results-empty">
                  Preparing comparison metrics from the current comparison pair.
                </p>
              )}

              {!needsPairwiseReduction && comparisonError && (
                <p className="time-waveform-workspace__comparison-results-error">{comparisonError}</p>
              )}

              {!needsPairwiseReduction && !isComparisonLoading && !comparisonError && comparisonResults && (
                <>
                  <div className="time-waveform-workspace__comparison-metrics">
                    {comparisonMetrics.map((metric) => (
                      <button
                        aria-controls="comparison-metric-evidence"
                        aria-expanded={activeMetric?.metricKey === metric.metricKey && isComparisonDetailsOpen}
                        aria-pressed={activeMetric?.metricKey === metric.metricKey}
                        key={metric.metricKey}
                        className={`time-waveform-workspace__comparison-metric-card${activeMetric?.metricKey === metric.metricKey ? ' time-waveform-workspace__comparison-metric-card--active' : ''}`}
                        type="button"
                        onClick={() => {
                          setSelectedMetricKey(metric.metricKey)
                          setIsComparisonDetailsOpen(true)
                        }}
                      >
                        <span className="time-waveform-workspace__comparison-metric-label">
                          {formatComparisonMetricLabel(metric.metricKey)}
                        </span>
                        <strong className="time-waveform-workspace__comparison-metric-value">
                          {formatAggregateValue(metric.meanDifference, metric.unit)}
                        </strong>
                        <span className="time-waveform-workspace__comparison-metric-meta">
                          Spread {formatAggregateValue(metric.spread, metric.unit)} · Pairs {metric.comparedPairCount}
                          {metric.missingValueCount > 0 ? ` · Missing ${metric.missingValueCount}` : ''}
                        </span>
                      </button>
                    ))}
                  </div>

                  {isComparisonDetailsOpen && (activeMetric || comparisonResults.limitations.length > 0) && (
                    <section
                      aria-label="Comparison details"
                      className="time-waveform-workspace__comparison-details"
                      id="comparison-metric-evidence"
                    >
                      <div className="time-waveform-workspace__comparison-details-toolbar">
                        <button
                          aria-controls="comparison-metric-evidence"
                          aria-expanded="true"
                          className="time-waveform-workspace__comparison-details-hide"
                          type="button"
                          onClick={() => setIsComparisonDetailsOpen(false)}
                        >
                          Hide evidence
                          <ChevronUp aria-hidden="true" size={14} />
                        </button>
                      </div>
                      {activeMetric && activeObservation && (
                        <section className="time-waveform-workspace__comparison-focus" aria-label="Selected metric evidence">
                          <div>
                            <span className="time-waveform-workspace__comparison-focus-kicker">Selected metric</span>
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
                            Largest absolute aligned-pair delta: {activeObservation.displayNameA} vs {activeObservation.displayNameB} ·
                            A {formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'A'), activeMetric.unit)} ·
                            B {formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'B'), activeMetric.unit)} ·
                            Delta {formatAggregateValue(getObservationDelta(activeObservation, activeMetric.metricKey), activeMetric.unit)}
                          </p>
                        </section>
                      )}

                      {comparisonResults.limitations.length > 0 && (
                        <section className="time-waveform-workspace__comparison-limitations" aria-label="Comparison limitations">
                          <p className="time-waveform-workspace__comparison-limitations-summary">
                            {coverageSummary.copy}
                          </p>
                          {comparisonResults.limitations.map((limitation) => (
                            <p key={`${limitation.code}-${limitation.detail}`}>
                              <strong>{formatLimitationLabel(limitation.code)}:</strong> {limitation.detail}
                            </p>
                          ))}
                        </section>
                      )}
                    </section>
                  )}
                </>
              )}
            </section>
          )}
          {regionOfInterest && layoutMode !== 'compare' && roiScopeLabel && (
            <section className="time-waveform-workspace__roi-summary" aria-label="Selected time region">
              <div className="time-waveform-workspace__roi-copy">
                <span className="time-waveform-workspace__roi-title">Selected region</span>
                <span className="time-waveform-workspace__roi-values">
                  {roiScopeLabel}
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
            compareEvidenceDetail={
              activeMetric && activeObservation
                ? `Δ ${formatAggregateValue(getObservationDelta(activeObservation, activeMetric.metricKey), activeMetric.unit)} · A ${formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'A'), activeMetric.unit)} · B ${formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'B'), activeMetric.unit)}`
                : null
            }
            compareEvidenceKicker={activeMetric ? 'Inspecting evidence for' : null}
            compareEvidenceScope={roiScopeLabel ? `ROI ${roiScopeLabel}` : null}
            compareEvidenceSummary={
              activeMetric && activeObservation
                ? `${activeObservation.displayNameA} vs ${activeObservation.displayNameB}`
                : null
            }
            compareEvidenceTitle={activeMetric ? formatComparisonMetricLabel(activeMetric.metricKey) : null}
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

type TComparisonCoverageTone = 'strong' | 'partial' | 'weak'

interface IComparisonCoverageSummary {
  alignedPairCount: number
  comparedPairCount: number
  copy: string
  label: string
  limitationCount: number
  missingValueCount: number
  tone: TComparisonCoverageTone
}

const getComparisonCoverageSummary = (
  comparisonResults: IRecordingComparisonResponse | null,
  activeMetric: IRecordingComparisonMetricAggregate | null
): IComparisonCoverageSummary => {
  if (!comparisonResults || !activeMetric) {
    return {
      alignedPairCount: 0,
      comparedPairCount: 0,
      copy: 'Coverage will appear once comparison metrics are available.',
      label: 'Coverage pending',
      limitationCount: 0,
      missingValueCount: 0,
      tone: 'weak',
    }
  }

  const alignedPairCount = comparisonResults.alignedSignals.length
  const comparedPairCount = activeMetric.comparedPairCount
  const missingValueCount = activeMetric.missingValueCount
  const limitationCount = comparisonResults.limitations.length
  const hasLowCoverageLimitation = comparisonResults.limitations.some((limitation) => limitation.code === 'LowCoverage')
  const hasMissingOrAmbiguousLimitation = comparisonResults.limitations.some(
    (limitation) => limitation.code === 'Missing' || limitation.code === 'Ambiguous'
  )

  if (hasLowCoverageLimitation || comparedPairCount <= 1) {
    return {
      alignedPairCount,
      comparedPairCount,
      copy: 'Interpret these metric deltas carefully. The current comparison rests on a very small amount of aligned evidence.',
      label: 'Weak evidence',
      limitationCount,
      missingValueCount,
      tone: 'weak',
    }
  }

  if (missingValueCount > 0 || hasMissingOrAmbiguousLimitation) {
    return {
      alignedPairCount,
      comparedPairCount,
      copy: 'Some aligned evidence is incomplete or missing for the selected metric.',
      label: 'Partial evidence',
      limitationCount,
      missingValueCount,
      tone: 'partial',
    }
  }

  return {
    alignedPairCount,
    comparedPairCount,
    copy: 'The selected metric is supported by the currently aligned evidence set.',
    label: 'Stronger evidence',
    limitationCount,
    missingValueCount,
    tone: 'strong',
  }
}
