import type {
  IAgentEvidenceReference,
  IAgentObservationScope,
  IAgentStructuredObservation,
} from '../types/copilot.types'

const METRIC_UNITS: Record<string, string> = {
  peakAmplitudeDelta: 'FS',
  rmsAmplitudeDelta: 'FS',
  crestFactorDelta: 'ratio',
  clippingSampleCountDelta: 'samples',
}

const OBSERVATION_ID_PATTERN = /^obs_v1_[0-9a-f]{24}$/

export const isStructuredObservationCollection = (
  value: unknown
): value is IAgentStructuredObservation[] => {
  if (value === undefined) return true
  if (!Array.isArray(value)) return false

  const observations = value as unknown[]
  const ids = new Set<string>()
  for (const observation of observations) {
    if (!isStructuredObservation(observation)) return false
    if (ids.has(observation.observationId)) return false
    ids.add(observation.observationId)
  }
  return true
}

const isStructuredObservation = (value: unknown): value is IAgentStructuredObservation => {
  if (!isRecord(value) ||
      typeof value.observationId !== 'string' ||
      !OBSERVATION_ID_PATTERN.test(value.observationId)) return false
  if (!['complete', 'limited', 'mixed'].includes(String(value.status))) return false
  if (!isObservationScope(value.scope) || !isStringArray(value.limitationCodes)) return false
  if (!Array.isArray(value.evidenceReferences) || value.evidenceReferences.length === 0) return false

  const observationId = value.observationId
  const scope = value.scope
  const references = value.evidenceReferences
  if (!references.every((reference) =>
    isEvidenceReference(reference, observationId, value.kind, scope))) return false

  if (value.kind === 'comparison_metric') {
    return value.signalFinding === null &&
      isComparisonMetric(value.comparisonMetric) &&
      references.every((reference) => metricReferenceMatches(reference, value.comparisonMetric))
  }
  if (value.kind === 'signal_finding') {
    return value.comparisonMetric === null &&
      value.status === 'complete' &&
      isSignalFinding(value.signalFinding) &&
      references.every((reference) => findingReferenceMatches(reference, value.signalFinding))
  }
  return false
}

const isComparisonMetric = (value: unknown) => {
  if (!isRecord(value) || !isNonEmptyString(value.metricKey) || !isNonEmptyString(value.metricLabel)) return false
  if (METRIC_UNITS[value.metricKey] !== value.unit) return false
  if (!isRecord(value.aggregate) || !isRecord(value.selectedPair)) return false

  const aggregate = value.aggregate
  const pair = value.selectedPair
  return isNonNegativeInteger(aggregate.comparedPairCount) &&
    isNonNegativeInteger(aggregate.missingValueCount) &&
    finiteNumbers(
      aggregate.meanDifference,
      aggregate.medianDifference,
      aggregate.minimumDifference,
      aggregate.maximumDifference,
      aggregate.spread,
    ) && Number(aggregate.spread) >= 0 &&
    nonEmptyStrings(
      pair.recordingIdA,
      pair.recordingFileNameA,
      pair.signalIdA,
      pair.signalDisplayNameA,
      pair.recordingIdB,
      pair.recordingFileNameB,
      pair.signalIdB,
      pair.signalDisplayNameB,
    ) && finiteNumbers(pair.valueA, pair.valueB, pair.difference)
}

const isSignalFinding = (value: unknown) =>
  isRecord(value) &&
  ['A', 'B'].includes(String(value.side)) &&
  nonEmptyStrings(
    value.recordingId,
    value.recordingFileName,
    value.signalId,
    value.signalDisplayName,
    value.category,
    value.severity,
    value.label,
  ) && (value.detail === null || typeof value.detail === 'string')

const metricReferenceMatches = (reference: unknown, metric: unknown) => {
  if (!isRecord(reference) || !isRecord(metric) || !isRecord(metric.selectedPair)) return false
  return reference.metricKey === metric.metricKey &&
    sameStringArray(reference.recordingIds, [metric.selectedPair.recordingIdA, metric.selectedPair.recordingIdB]) &&
    sameStringArray(reference.signalIds, [metric.selectedPair.signalIdA, metric.selectedPair.signalIdB])
}

const findingReferenceMatches = (reference: unknown, finding: unknown) => {
  if (!isRecord(reference) || !isRecord(finding)) return false
  return sameStringArray(reference.recordingIds, [finding.recordingId]) &&
    sameStringArray(reference.signalIds, [finding.signalId])
}

const isEvidenceReference = (
  value: unknown,
  observationId: string,
  kind: unknown,
  scope: IAgentObservationScope,
): value is IAgentEvidenceReference => {
  if (!isRecord(value)) return false
  if (value.referenceId !== observationId || value.evidenceType !== kind) return false
  if (!isStringArray(value.recordingIds) || value.recordingIds.length === 0) return false
  if (!isStringArray(value.signalIds) || value.signalIds.length === 0) return false
  if (!isObservationScope(value.scope) || !sameScope(value.scope, scope)) return false
  return kind === 'comparison_metric'
    ? isNonEmptyString(value.metricKey)
    : value.metricKey === null
}

const isObservationScope = (value: unknown): value is IAgentObservationScope => {
  if (!isRecord(value)) return false
  if (value.kind === 'full_duration') {
    return value.startTimeSeconds === null && value.endTimeSeconds === null
  }
  return value.kind === 'roi' &&
    finiteNumbers(value.startTimeSeconds, value.endTimeSeconds) &&
    Number(value.startTimeSeconds) >= 0 &&
    Number(value.endTimeSeconds) > Number(value.startTimeSeconds)
}

const sameScope = (left: IAgentObservationScope, right: IAgentObservationScope) =>
  left.kind === right.kind &&
  left.startTimeSeconds === right.startTimeSeconds &&
  left.endTimeSeconds === right.endTimeSeconds

const isRecord = (value: unknown): value is Record<string, unknown> =>
  Boolean(value) && typeof value === 'object'

const isNonEmptyString = (value: unknown): value is string =>
  typeof value === 'string' && value.trim().length > 0

const isStringArray = (value: unknown): value is string[] =>
  Array.isArray(value) && value.every(isNonEmptyString)

const sameStringArray = (value: unknown, expected: unknown[]) =>
  Array.isArray(value) &&
  value.length === expected.length &&
  value.every((item, index) => item === expected[index])

const isNonNegativeInteger = (value: unknown) =>
  Number.isInteger(value) && Number(value) >= 0

const finiteNumbers = (...values: unknown[]) =>
  values.every((value) => typeof value === 'number' && Number.isFinite(value))

const nonEmptyStrings = (...values: unknown[]) => values.every(isNonEmptyString)
