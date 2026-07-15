import { AnalysisWorkspaceChart } from './AnalysisWorkspaceChart'
import { AnalysisWorkspaceHeader } from './AnalysisWorkspaceHeader'
import { ComparisonEvidenceInspector } from './ComparisonEvidenceInspector'
import { ComparisonReportDialog } from '../../report/components/ComparisonReportDialog'
import { AudioTransport } from '../../playback/components/AudioTransport'
import { RecordingRail } from '../../recording-rail/components/RecordingRail'
import { useAnalysisWorkspaceMetrics } from '../../metrics/hooks/useAnalysisWorkspaceMetrics'
import { useReportExport } from '../../report/hooks/useReportExport'
import { formatCompactDuration } from '../../utils/analysisWorkspaceFormatting'
import { useAnalysisWorkspacePanels } from '../hooks/useAnalysisWorkspacePanels'
import { useTimeWaveformWorkspace } from '../hooks/useTimeWaveformWorkspace'
import { getRecordingComparison } from '../../services/recordingComparison'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import { getComparisonSetupSummary } from '../../utils/analysisWorkspaceState'
import {
  formatAggregateValue,
  formatComparisonMetricLabel,
  getComparisonCoverageSummary,
  getObservationDelta,
  getObservationValue,
} from '../../utils/comparisonEvidence'
import type { IImportedFileSummary } from '../../../../common/contracts/import'
import type {
  IComparisonCopilotSelection,
  IRecordingComparisonMetricAggregate,
  IRecordingComparisonResponse,
} from '../../types'
import { PanelRightOpen } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
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

const COPILOT_CLOSE_TRANSITION_MS = 220

const TimeWaveformWorkspace = ({ importedFiles, isCopilotOpen, onCopilotToggle }: ITimeWaveformWorkspaceProps) => {
  const setComparisonCopilotContext = useAnalysisWorkspaceStore((state) => state.setComparisonCopilotContext)
  const [comparisonRequestState, setComparisonRequestState] = useState<IComparisonRequestState>({
    error: null,
    requestKey: null,
    results: null,
  })
  const [isEvidenceInspectorOpen, setIsEvidenceInspectorOpen] = useState(false)
  const [isCopilotHandoffActive, setIsCopilotHandoffActive] = useState(false)
  const evidenceInspectorTriggerRef = useRef<HTMLElement | null>(null)
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
    onComparisonTargetsSwap,
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
  const activePairRecordingA = groupARecordings.length === 1 ? groupARecordings[0] : null
  const activePairRecordingB = groupBRecordings.length === 1 ? groupBRecordings[0] : null
  const activePairRecordingIdA = activePairRecordingA?.recordingId ?? null
  const activePairRecordingIdB = activePairRecordingB?.recordingId ?? null
  const comparisonRoiStartSeconds = regionOfInterest?.startTimeSeconds ?? null
  const comparisonRoiEndSeconds = regionOfInterest?.endTimeSeconds ?? null
  const canRequestPairwiseComparison =
    layoutMode === 'compare' && comparisonSetup.state === 'valid' && groupARecordings.length === 1 && groupBRecordings.length === 1
  const activeComparisonRequestKey = canRequestPairwiseComparison
    ? [
        activePairRecordingIdA,
        activePairRecordingIdB,
        comparisonRoiStartSeconds ?? 'full',
        comparisonRoiEndSeconds ?? 'full',
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
            copy: 'Pair selected.',
          }
      : comparisonSetup.state === 'conflict'
        ? {
            label: 'Resolve pair',
            copy: 'Choose exactly one recording for each target.',
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
    comparisonReportFormat,
    comparisonReportTitle,
    excludedRecordings,
    handleComparisonReportExport,
    handleExportReport,
    isComparisonReportOpen,
    isExporting,
    setComparisonReportTitle,
    setComparisonReportFormat,
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
    if (
      !canRequestPairwiseComparison ||
      !activeComparisonRequestKey ||
      !activePairRecordingIdA ||
      !activePairRecordingIdB
    ) {
      return
    }

    let isCurrent = true

    void getRecordingComparison(
      activePairRecordingIdA,
      activePairRecordingIdB,
      comparisonRoiStartSeconds !== null && comparisonRoiEndSeconds !== null
        ? {
            startTimeSeconds: comparisonRoiStartSeconds,
            endTimeSeconds: comparisonRoiEndSeconds,
          }
        : null
    )
      .then((response) => {
        if (!isCurrent) {
          return
        }

        setComparisonRequestState({
          error: null,
          requestKey: activeComparisonRequestKey,
          results: response,
        })
        setIsEvidenceInspectorOpen(false)
        setIsCopilotHandoffActive(false)
      })
      .catch((caughtError) => {
        if (!isCurrent) {
          return
        }

        setComparisonRequestState({
          error: caughtError instanceof Error ? caughtError.message : 'Comparison results could not be prepared.',
          requestKey: activeComparisonRequestKey,
          results: null,
        })
        setIsEvidenceInspectorOpen(false)
        setIsCopilotHandoffActive(false)
      })

    return () => {
      isCurrent = false
    }
  }, [
    activeComparisonRequestKey,
    activePairRecordingIdA,
    activePairRecordingIdB,
    canRequestPairwiseComparison,
    comparisonRoiEndSeconds,
    comparisonRoiStartSeconds,
  ])

  useEffect(() => {
    setComparisonCopilotContext(comparisonCopilotContext)

    return () => {
      setComparisonCopilotContext(null)
    }
  }, [comparisonCopilotContext, setComparisonCopilotContext])

  useEffect(() => {
    if (!isCopilotHandoffActive || isCopilotOpen) {
      return
    }

    const timeoutId = window.setTimeout(() => {
      setIsCopilotHandoffActive(false)
    }, COPILOT_CLOSE_TRANSITION_MS)

    return () => window.clearTimeout(timeoutId)
  }, [isCopilotHandoffActive, isCopilotOpen])

  const handleEvidenceInspectorOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setIsEvidenceInspectorOpen(false)
      setIsCopilotHandoffActive(false)
      return
    }

    if (isCopilotOpen) {
      if (isCopilotHandoffActive) {
        return
      }

      setIsCopilotHandoffActive(true)
      setIsEvidenceInspectorOpen(true)
      onCopilotToggle()
      return
    }

    setIsEvidenceInspectorOpen(true)
  }

  const handleLayoutModeChange = (mode: typeof layoutMode) => {
    if (mode !== 'compare') {
      setIsEvidenceInspectorOpen(false)
      setIsCopilotHandoffActive(false)
    }

    onLayoutModeChange(mode)
  }

  const handleRecordingGroupAssignment = (
    recordingId: Parameters<typeof onRecordingGroupAssignment>[0],
    assignment: Parameters<typeof onRecordingGroupAssignment>[1]
  ) => {
    setIsEvidenceInspectorOpen(false)
    setIsCopilotHandoffActive(false)
    onRecordingGroupAssignment(recordingId, assignment)
  }

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
        onLayoutModeChange={handleLayoutModeChange}
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
          format={comparisonReportFormat}
          isExporting={isExporting}
          isOpen={isComparisonReportOpen}
          onExport={handleComparisonReportExport}
          onFormatChange={setComparisonReportFormat}
          onOpenChange={setIsComparisonReportOpen}
          onTitleChange={setComparisonReportTitle}
          regionOfInterest={regionOfInterest}
          title={comparisonReportTitle}
        />
      )}

      {activeMetric && activePairRecordingA && activePairRecordingB && comparisonResults && (
        <ComparisonEvidenceInspector
          activeMetric={activeMetric}
          activeObservation={activeObservation}
          coverageSummary={coverageSummary}
          fileNameA={activePairRecordingA.fileName}
          fileNameB={activePairRecordingB.fileName}
          isOpen={isEvidenceInspectorOpen}
          limitations={comparisonResults.limitations}
          onOpenChange={handleEvidenceInspectorOpenChange}
          preventOutsideDismiss={isCopilotHandoffActive}
          returnFocusRef={evidenceInspectorTriggerRef}
          roiScopeLabel={roiScopeLabel}
        />
      )}

      <div className="time-waveform-workspace__body">
        <RecordingRail
          expandedRecordings={expandedRecordings}
          onComparisonTargetsSwap={onComparisonTargetsSwap}
          onRecordingGroupAssignment={handleRecordingGroupAssignment}
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
                {comparisonGuidance.copy}
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
                        <button
                          aria-haspopup="dialog"
                          className="time-waveform-workspace__comparison-results-details-toggle"
                          data-evidence-inspector-trigger
                          type="button"
                          onClick={(event) => {
                            evidenceInspectorTriggerRef.current = event.currentTarget
                            handleEvidenceInspectorOpenChange(true)
                          }}
                        >
                          <PanelRightOpen aria-hidden="true" size={14} />
                          Evidence &amp; limitations
                        </button>
                      </>
                    )}
                  </div>
                )}
              </div>

              {isComparisonLoading && (
                <p className="time-waveform-workspace__comparison-results-empty">
                  Preparing comparison metrics from the current comparison pair.
                </p>
              )}

              {comparisonError && (
                <p className="time-waveform-workspace__comparison-results-error">{comparisonError}</p>
              )}

              {!isComparisonLoading && !comparisonError && comparisonResults && (
                <>
                  <div className="time-waveform-workspace__comparison-metrics">
                    {comparisonMetrics.map((metric) => (
                      <button
                        aria-haspopup="dialog"
                        aria-pressed={activeMetric?.metricKey === metric.metricKey}
                        data-evidence-inspector-trigger
                        key={metric.metricKey}
                        className={`time-waveform-workspace__comparison-metric-card${activeMetric?.metricKey === metric.metricKey ? ' time-waveform-workspace__comparison-metric-card--active' : ''}`}
                        type="button"
                        onClick={(event) => {
                          evidenceInspectorTriggerRef.current = event.currentTarget
                          setSelectedMetricKey(metric.metricKey)

                          if (!isCopilotOpen) {
                            handleEvidenceInspectorOpenChange(true)
                          }
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
          <AudioTransport
            recordings={recordings}
            recordingGroupAssignments={recordingGroupAssignments}
          />
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
