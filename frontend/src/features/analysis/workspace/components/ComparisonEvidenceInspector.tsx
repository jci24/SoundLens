import * as Dialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  formatAggregateValue,
  formatComparisonMetricLabel,
  formatLimitationLabel,
  getObservationDelta,
  getObservationValue,
} from '../../utils/comparisonEvidence'
import type {
  IRecordingComparisonLimitation,
  IRecordingComparisonMetricAggregate,
  IRecordingComparisonSignalObservation,
  IRecordingComparisonIntegrityAssessment,
} from '../../types'
import type { IComparisonCoverageSummary } from '../../utils/comparisonEvidence'
import type { RefObject } from 'react'
import './ComparisonEvidenceInspector.scss'

interface IComparisonEvidenceInspectorProps {
  activeMetric: IRecordingComparisonMetricAggregate
  activeObservation: IRecordingComparisonSignalObservation | null
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
              <span>{integrityAssessment.status === 'limited' ? 'Limited' : 'Structurally matched'}</span>
            </div>
            <ul className="comparison-evidence-inspector__integrity">
              {integrityAssessment.checks.map((check) => (
                <li key={check.code}>
                  <div>
                    <strong>{check.label}</strong>
                    <span>{check.status}</span>
                  </div>
                  <p>{check.detail}</p>
                </li>
              ))}
            </ul>
          </section>

          <section aria-labelledby="comparison-evidence-limitations-title" className="comparison-evidence-inspector__section">
            <div className="comparison-evidence-inspector__section-heading">
              <h3 id="comparison-evidence-limitations-title">Limitations</h3>
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
              <p className="comparison-evidence-inspector__empty">No additional comparison limitations were reported.</p>
            )}
          </section>
        </div>
      </Dialog.Content>
    </Dialog.Portal>
  </Dialog.Root>
)

export { ComparisonEvidenceInspector }
