import type {
  IRecordingComparisonMetricAggregate,
  IRecordingComparisonResponse,
  IRecordingComparisonSignalObservation,
  IRecordingComparisonIntegrityAssessment,
} from '../types'

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

const formatComparisonIntegrityDetails = (assessment: IRecordingComparisonIntegrityAssessment) => {
  const isCalibrationUnknown = assessment.checks.some(
    (check) => check.code === 'Calibration' && check.status === 'unknown'
  )
  const notes = [
    assessment.limitedCheckCount > 0
      ? `${assessment.limitedCheckCount} limited`
      : null,
    isCalibrationUnknown
      ? 'Calibration unknown'
      : assessment.unknownCheckCount > 0
        ? `${assessment.unknownCheckCount} unknown`
      : null,
  ].filter(Boolean)

  return notes.length > 0 ? notes.join(' · ') : 'All checks matched'
}

const formatComparisonIntegritySummary = (assessment: IRecordingComparisonIntegrityAssessment) =>
  `Comparison context · ${formatComparisonIntegrityDetails(assessment)}`

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

export {
  formatAggregateValue,
  formatComparisonMetricLabel,
  formatComparisonIntegritySummary,
  formatComparisonIntegrityDetails,
  formatLimitationLabel,
  getComparisonCoverageSummary,
  getObservationDelta,
  getObservationValue,
}
export type { IComparisonCoverageSummary }
