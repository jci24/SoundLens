import * as Dialog from '@radix-ui/react-dialog'
import { Copy, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  formatAggregateValue,
  formatComparisonMetricLabel,
  formatLimitationLabel,
  getObservationDelta,
  getObservationValue,
} from '../../utils/comparisonEvidence'
import type {
  IRecordingComparisonAnalysisProvenance,
  IRecordingComparisonAnalysisSpecification,
  IRecordingComparisonLimitation,
  IRecordingComparisonMetricAggregate,
  IRecordingComparisonSignalObservation,
  IRecordingComparisonIntegrityAssessment,
} from '../../types'
import type { IComparisonCoverageSummary } from '../../utils/comparisonEvidence'
import { useState, type RefObject } from 'react'
import './ComparisonEvidenceInspector.scss'

const formatIntegrityStatus = (status: IRecordingComparisonIntegrityAssessment['checks'][number]['status']) => {
  switch (status) {
    case 'matched':
      return 'Matched'
    case 'limited':
      return 'Review'
    case 'unknown':
      return 'Unknown'
  }
}

const formatIntegrityHeading = (assessment: IRecordingComparisonIntegrityAssessment) => {
  const isCalibrationUnknown = assessment.checks.some(
    (check) => check.code === 'Calibration' && check.status === 'unknown'
  )
  const parts = [
    assessment.limitedCheckCount > 0
      ? `${assessment.limitedCheckCount} limitation${assessment.limitedCheckCount === 1 ? '' : 's'}`
      : null,
    isCalibrationUnknown
      ? 'Calibration unknown'
      : assessment.unknownCheckCount > 0
        ? `${assessment.unknownCheckCount} unknown`
      : null,
  ].filter(Boolean)

  return parts.length > 0 ? parts.join(' · ') : 'All checks matched'
}

const shortenFingerprint = (value: string) => `${value.slice(0, 19)}…${value.slice(-8)}`

const FingerprintValue = ({ label, value }: { label: string; value: string }) => {
  const [copyStatus, setCopyStatus] = useState<'idle' | 'copied' | 'failed'>('idle')

  const copyValue = async () => {
    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Clipboard API unavailable.')
      }

      await navigator.clipboard.writeText(value)
      setCopyStatus('copied')
    } catch {
      setCopyStatus('failed')
    }
  }

  return (
    <span className="comparison-evidence-inspector__fingerprint">
      <code title={value}>{shortenFingerprint(value)}</code>
      <Button aria-label={`Copy full ${label}`} onClick={copyValue} size="icon-sm" type="button" variant="ghost">
        <Copy aria-hidden="true" />
      </Button>
      {copyStatus === 'copied' && <small role="status">Copied</small>}
      {copyStatus === 'failed' && <small role="alert">Copy unavailable</small>}
    </span>
  )
}

interface IComparisonEvidenceInspectorProps {
  activeMetric: IRecordingComparisonMetricAggregate
  activeObservation: IRecordingComparisonSignalObservation | null
  analysisProvenance: IRecordingComparisonAnalysisProvenance
  analysisSpecification: IRecordingComparisonAnalysisSpecification
  coverageSummary: IComparisonCoverageSummary
  fileNameA: string
  fileNameB: string
  isOpen: boolean
  integrityAssessment: IRecordingComparisonIntegrityAssessment
  limitations: IRecordingComparisonLimitation[]
  onOpenChange: (isOpen: boolean) => void
  preventOutsideDismiss: boolean
  returnFocusRef: RefObject<HTMLElement | null>
  roiScopeLabel: string | null
}

const ComparisonEvidenceInspector = ({
  activeMetric,
  activeObservation,
  analysisProvenance,
  analysisSpecification,
  coverageSummary,
  fileNameA,
  fileNameB,
  isOpen,
  integrityAssessment,
  limitations,
  onOpenChange,
  preventOutsideDismiss,
  returnFocusRef,
  roiScopeLabel,
}: IComparisonEvidenceInspectorProps) => (
  <Dialog.Root modal={false} onOpenChange={onOpenChange} open={isOpen}>
    <Dialog.Portal>
      <Dialog.Content
        className="comparison-evidence-inspector__content"
        onCloseAutoFocus={(event) => {
          event.preventDefault()
          returnFocusRef.current?.focus()
        }}
        onFocusOutside={(event) => event.preventDefault()}
        onPointerDownOutside={(event) => {
          if (preventOutsideDismiss) {
            event.preventDefault()
            return
          }

          const target = event.detail.originalEvent.target

          if (target instanceof Element && target.closest('[data-evidence-inspector-trigger]')) {
            event.preventDefault()
          }
        }}
      >
        <header className="comparison-evidence-inspector__header">
          <div>
            <span className="comparison-evidence-inspector__kicker">Selected metric evidence</span>
            <Dialog.Title className="comparison-evidence-inspector__title">
              {formatComparisonMetricLabel(activeMetric.metricKey)}
            </Dialog.Title>
            <Dialog.Description className="comparison-evidence-inspector__description">
              Backend-computed evidence for the active comparison.
            </Dialog.Description>
          </div>
          <Dialog.Close asChild>
            <Button aria-label="Close evidence inspector" size="icon-sm" type="button" variant="ghost">
              <X aria-hidden="true" />
            </Button>
          </Dialog.Close>
        </header>

        <div className="comparison-evidence-inspector__body">
          <dl className="comparison-evidence-inspector__scope">
            <div>
              <dt>Compare A</dt>
              <dd>{fileNameA}</dd>
            </div>
            <div>
              <dt>Compare B</dt>
              <dd>{fileNameB}</dd>
            </div>
            <div>
              <dt>Scope</dt>
              <dd>{roiScopeLabel ? `ROI ${roiScopeLabel}` : 'Full duration'}</dd>
            </div>
          </dl>

          <section aria-labelledby="comparison-evidence-summary-title" className="comparison-evidence-inspector__section">
            <div className="comparison-evidence-inspector__section-heading">
              <h3 id="comparison-evidence-summary-title">Metric summary</h3>
              <span>{coverageSummary.label}</span>
            </div>
            <dl className="comparison-evidence-inspector__stats">
              <div>
                <dt>Mean delta A-B</dt>
                <dd>{formatAggregateValue(activeMetric.meanDifference, activeMetric.unit)}</dd>
              </div>
              <div>
                <dt>Median</dt>
                <dd>{formatAggregateValue(activeMetric.medianDifference, activeMetric.unit)}</dd>
              </div>
              <div>
                <dt>Spread</dt>
                <dd>{formatAggregateValue(activeMetric.spread, activeMetric.unit)}</dd>
              </div>
              <div>
                <dt>Coverage</dt>
                <dd>{activeMetric.comparedPairCount} pair{activeMetric.comparedPairCount === 1 ? '' : 's'}</dd>
              </div>
              <div>
                <dt>Missing</dt>
                <dd>{activeMetric.missingValueCount}</dd>
              </div>
            </dl>
          </section>

          {activeObservation && (
            <section aria-labelledby="comparison-evidence-pair-title" className="comparison-evidence-inspector__section">
              <div className="comparison-evidence-inspector__section-heading">
                <h3 id="comparison-evidence-pair-title">Selected aligned pair</h3>
              </div>
              <p className="comparison-evidence-inspector__pair-name">
                {activeObservation.displayNameA} vs {activeObservation.displayNameB}
              </p>
              <dl className="comparison-evidence-inspector__pair-values">
                <div>
                  <dt>Compare A</dt>
                  <dd>{formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'A'), activeMetric.unit)}</dd>
                </div>
                <div>
                  <dt>Compare B</dt>
                  <dd>{formatAggregateValue(getObservationValue(activeObservation, activeMetric.metricKey, 'B'), activeMetric.unit)}</dd>
                </div>
                <div>
                  <dt>Delta A-B</dt>
                  <dd>{formatAggregateValue(getObservationDelta(activeObservation, activeMetric.metricKey), activeMetric.unit)}</dd>
                </div>
              </dl>
            </section>
          )}

          <section aria-labelledby="comparison-evidence-integrity-title" className="comparison-evidence-inspector__section">
            <div className="comparison-evidence-inspector__section-heading">
              <h3 id="comparison-evidence-integrity-title">Comparison context</h3>
              <span>{formatIntegrityHeading(integrityAssessment)}</span>
            </div>
            <ul className="comparison-evidence-inspector__integrity">
              {integrityAssessment.checks.map((check) => (
                <li key={check.code}>
                  <div>
                    <strong>{check.label}</strong>
                    <span>{formatIntegrityStatus(check.status)}</span>
                  </div>
                  <p>{check.detail}</p>
                </li>
              ))}
            </ul>
          </section>

          <details className="comparison-evidence-inspector__methods">
            <summary>
              <span>Analysis methods</span>
              <small>{analysisSpecification.contractVersion}</small>
            </summary>
            <div className="comparison-evidence-inspector__methods-content">
              <dl>
                <div>
                  <dt>Scope</dt>
                  <dd>{analysisSpecification.scope === 'roi' ? 'Selected ROI' : 'Full duration'}</dd>
                </div>
                <div>
                  <dt>Difference</dt>
                  <dd>Compare A minus Compare B</dd>
                </div>
                <div>
                  <dt>Aggregates</dt>
                  <dd>Mean, median, minimum, maximum, and spread</dd>
                </div>
              </dl>
              <ul>
                {analysisSpecification.metricMethods.map((method) => (
                  <li key={method.metricKey}>
                    <div>
                      <strong>{method.label}</strong>
                      <span>{method.unit}</span>
                    </div>
                    <p>{method.definition}</p>
                    <code>{method.methodId}@{method.methodVersion}</code>
                  </li>
                ))}
              </ul>
            </div>
          </details>

          <details className="comparison-evidence-inspector__provenance">
            <summary>
              <span>Provenance</span>
              <small>{analysisProvenance.contractVersion}</small>
            </summary>
            <div className="comparison-evidence-inspector__provenance-content">
              <dl>
                <div>
                  <dt>Compare A</dt>
                  <dd><FingerprintValue key={analysisProvenance.recordingA.value} label="Compare A content fingerprint" value={analysisProvenance.recordingA.value} /></dd>
                </div>
                <div>
                  <dt>Compare B</dt>
                  <dd><FingerprintValue key={analysisProvenance.recordingB.value} label="Compare B content fingerprint" value={analysisProvenance.recordingB.value} /></dd>
                </div>
                <div>
                  <dt>Implementation</dt>
                  <dd><code>{analysisProvenance.implementationId}@{analysisProvenance.implementationVersion}</code></dd>
                </div>
                <div>
                  <dt>Build</dt>
                  <dd><code>{analysisProvenance.applicationBuildVersion}</code></dd>
                </div>
                <div>
                  <dt>Decoder</dt>
                  <dd><code>{analysisProvenance.decoderId}@{analysisProvenance.decoderVersion}</code></dd>
                </div>
                <div>
                  <dt>Scope</dt>
                  <dd>{analysisProvenance.scope === 'roi' && analysisProvenance.regionOfInterest
                    ? `${analysisProvenance.regionOfInterest.startTimeSeconds} s to ${analysisProvenance.regionOfInterest.endTimeSeconds} s`
                    : 'Full duration'}</dd>
                </div>
                <div>
                  <dt>Methods</dt>
                  <dd>{analysisProvenance.methods.map((method) => (
                    <code key={method.methodId}>{method.methodId}@{method.methodVersion}</code>
                  ))}</dd>
                </div>
                <div>
                  <dt>Parameters</dt>
                  <dd><FingerprintValue key={analysisProvenance.parameterFingerprint} label="parameter fingerprint" value={analysisProvenance.parameterFingerprint} /></dd>
                </div>
                <div>
                  <dt>Evidence</dt>
                  <dd><FingerprintValue key={analysisProvenance.evidenceFingerprint} label="evidence fingerprint" value={analysisProvenance.evidenceFingerprint} /></dd>
                </div>
              </dl>
              <p>Provenance limitations</p>
              <ul>
                {analysisProvenance.limitations.map((limitation) => (
                  <li key={limitation.code}>{limitation.detail}</li>
                ))}
              </ul>
            </div>
          </details>

          <section aria-labelledby="comparison-evidence-limitations-title" className="comparison-evidence-inspector__section">
            <div className="comparison-evidence-inspector__section-heading">
              <h3 id="comparison-evidence-limitations-title">Metric evidence limitations</h3>
              <span>{limitations.length}</span>
            </div>
            <p className="comparison-evidence-inspector__coverage-copy">{coverageSummary.copy}</p>
            {limitations.length > 0 ? (
              <ul className="comparison-evidence-inspector__limitations">
                {limitations.map((limitation) => (
                  <li key={`${limitation.code}-${limitation.detail}`}>
                    <strong>{formatLimitationLabel(limitation.code)}</strong>
                    <span>{limitation.detail}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="comparison-evidence-inspector__empty">No metric-specific limitations were reported.</p>
            )}
          </section>
        </div>
      </Dialog.Content>
    </Dialog.Portal>
  </Dialog.Root>
)

export { ComparisonEvidenceInspector }
