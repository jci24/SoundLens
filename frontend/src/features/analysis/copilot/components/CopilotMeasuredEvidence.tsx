import { useId, useState } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'
import type {
  IAgentComparisonMetricObservation,
  IAgentObservationScope,
  IAgentSignalFindingObservation,
  IAgentStructuredObservation,
  TAgentStructuredObservationStatus,
} from '../types/copilot.types'
import './CopilotMeasuredEvidence.scss'

interface ICopilotMeasuredEvidenceProps {
  observations: IAgentStructuredObservation[]
}

const STATUS_LABELS: Record<TAgentStructuredObservationStatus, string> = {
  complete: 'Complete',
  limited: 'Limited',
  mixed: 'Mixed directions',
}

const numberFormatter = new Intl.NumberFormat(undefined, {
  maximumFractionDigits: 3,
})

const CopilotMeasuredEvidence = ({ observations }: ICopilotMeasuredEvidenceProps) => {
  const [isOpen, setIsOpen] = useState(false)
  const contentId = useId()

  if (observations.length === 0) return null

  return (
    <section className="copilot-measured-evidence" aria-label="Measured evidence">
      <button
        aria-controls={contentId}
        aria-expanded={isOpen}
        className="copilot-measured-evidence__toggle"
        type="button"
        onClick={() => setIsOpen((current) => !current)}
      >
        <span>Measured evidence</span>
        <span className="copilot-measured-evidence__count">
          {observations.length} observation{observations.length === 1 ? '' : 's'}
        </span>
        {isOpen ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
      </button>

      {isOpen && (
        <div className="copilot-measured-evidence__content" id={contentId}>
          {observations.map((observation) => observation.kind === 'comparison_metric'
            ? <MetricObservation key={observation.observationId} observation={observation} />
            : <FindingObservation key={observation.observationId} observation={observation} />)}
        </div>
      )}
    </section>
  )
}

const MetricObservation = ({ observation }: { observation: IAgentComparisonMetricObservation }) => {
  const metric = observation.comparisonMetric
  const pair = metric.selectedPair

  return (
    <article className="copilot-measured-evidence__observation">
      <div className="copilot-measured-evidence__heading">
        <strong>{metric.metricLabel}</strong>
        <span>{STATUS_LABELS[observation.status]}</span>
      </div>
      <dl className="copilot-measured-evidence__values">
        <EvidenceValue label="Scope" value={formatScope(observation.scope)} />
        <EvidenceValue label="Mean A-B" value={formatMeasurement(metric.aggregate.meanDifference, metric.unit)} />
        <EvidenceValue label="Median" value={formatMeasurement(metric.aggregate.medianDifference, metric.unit)} />
        <EvidenceValue label="Spread" value={formatMeasurement(metric.aggregate.spread, metric.unit)} />
        <EvidenceValue
          label="Coverage"
          value={`${metric.aggregate.comparedPairCount} pair${metric.aggregate.comparedPairCount === 1 ? '' : 's'}`}
        />
        <EvidenceValue label="Missing" value={String(metric.aggregate.missingValueCount)} />
      </dl>
      <p className="copilot-measured-evidence__pair">
        {pair.recordingFileNameA} · {pair.signalDisplayNameA} vs {pair.recordingFileNameB} · {pair.signalDisplayNameB}
      </p>
      <p className="copilot-measured-evidence__pair-values">
        A {formatMeasurement(pair.valueA, metric.unit)} · B {formatMeasurement(pair.valueB, metric.unit)} · Δ {formatMeasurement(pair.difference, metric.unit)}
      </p>
      <ObservationFooter observation={observation} />
    </article>
  )
}

const FindingObservation = ({ observation }: { observation: IAgentSignalFindingObservation }) => {
  const finding = observation.signalFinding

  return (
    <article className="copilot-measured-evidence__observation">
      <div className="copilot-measured-evidence__heading">
        <strong>{finding.label}</strong>
        <span>Compare {finding.side}</span>
      </div>
      <p className="copilot-measured-evidence__finding-signal">
        {finding.recordingFileName} · {finding.signalDisplayName}
      </p>
      {finding.detail && <p className="copilot-measured-evidence__finding-detail">{finding.detail}</p>}
      <p className="copilot-measured-evidence__finding-meta">{finding.category} · {finding.severity}</p>
      <ObservationFooter observation={observation} />
    </article>
  )
}

const EvidenceValue = ({ label, value }: { label: string; value: string }) => (
  <div>
    <dt>{label}</dt>
    <dd>{value}</dd>
  </div>
)

const ObservationFooter = ({ observation }: { observation: IAgentStructuredObservation }) => (
  <div className="copilot-measured-evidence__footer">
    {observation.limitationCodes.length > 0 && (
      <span>Limits: {observation.limitationCodes.join(', ')}</span>
    )}
    <code title={observation.observationId}>{shortenReference(observation.observationId)}</code>
  </div>
)

const formatScope = (scope: IAgentObservationScope) => scope.kind === 'full_duration'
  ? 'Full duration'
  : `${numberFormatter.format(scope.startTimeSeconds ?? 0)}–${numberFormatter.format(scope.endTimeSeconds ?? 0)} s`

const formatMeasurement = (value: number, unit: string) =>
  `${numberFormatter.format(value)} ${unit}`

const shortenReference = (reference: string) =>
  reference.length > 18 ? `${reference.slice(0, 15)}…` : reference

export { CopilotMeasuredEvidence }
