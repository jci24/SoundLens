import { API_BASE_URL } from '../../../common/api/config'
import type { IRequestedRegionOfInterest } from '../utils/analysisWorkspaceState'
import type {
  IRecordingComparisonAnalysisSpecification,
  IRecordingComparisonIntegrityAssessment,
  IRecordingComparisonIntegrityCheck,
  IRecordingComparisonMetricMethod,
  IRecordingComparisonResponse,
} from '../types'

const integrityCheckCodes = new Set<IRecordingComparisonIntegrityCheck['code']>([
  'SampleRate',
  'DurationScope',
  'SignalAlignment',
  'Calibration',
])
const integrityCheckStatuses = new Set<IRecordingComparisonIntegrityCheck['status']>([
  'matched',
  'limited',
  'unknown',
])
const expectedMetricMethods: ReadonlyArray<Pick<IRecordingComparisonMetricMethod, 'metricKey' | 'unit' | 'methodId'>> = [
  { metricKey: 'peakAmplitudeDelta', unit: 'FS', methodId: 'normalized_peak_amplitude' },
  { metricKey: 'rmsAmplitudeDelta', unit: 'FS', methodId: 'normalized_rms_amplitude' },
  { metricKey: 'crestFactorDelta', unit: 'ratio', methodId: 'peak_to_rms_ratio' },
  { metricKey: 'clippingSampleCountDelta', unit: 'samples', methodId: 'decoded_full_scale_sample_count' },
]

export const getRecordingComparison = async (
  recordingIdA: string,
  recordingIdB: string,
  regionOfInterest?: IRequestedRegionOfInterest | null
): Promise<IRecordingComparisonResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/comparisons/recordings`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      recordingIdA,
      recordingIdB,
      startTimeSeconds: regionOfInterest?.startTimeSeconds ?? null,
      endTimeSeconds: regionOfInterest?.endTimeSeconds ?? null,
    }),
  })

  if (!response.ok) {
    throw new Error(await readComparisonError(response))
  }

  return parseRecordingComparisonResponse(await response.json())
}

const parseRecordingComparisonResponse = (value: unknown): IRecordingComparisonResponse => {
  if (!isRecord(value) || !isIntegrityAssessment(value.integrityAssessment)) {
    throw new Error('Comparison results returned an invalid integrity assessment.')
  }

  if (!isAnalysisSpecification(value.analysisSpecification) ||
      !hasMatchingAnalysisScope(value.analysisSpecification, value.regionOfInterest) ||
      !hasMatchingAggregateMetrics(value.analysisSpecification, value.aggregateMetrics)) {
    throw new Error('Comparison results returned an invalid analysis specification.')
  }

  return value as unknown as IRecordingComparisonResponse
}

const isIntegrityAssessment = (value: unknown): value is IRecordingComparisonIntegrityAssessment => {
  if (
    !isRecord(value) ||
    (value.status !== 'complete' && value.status !== 'limited') ||
    !Number.isInteger(value.limitedCheckCount) ||
    !Number.isInteger(value.unknownCheckCount) ||
    !Array.isArray(value.checks) ||
    value.checks.length !== integrityCheckCodes.size
  ) {
    return false
  }

  const checks = value.checks.filter(isIntegrityCheck)
  const codes = new Set(checks.map((check) => check.code))
  const limitedCount = checks.filter((check) => check.status === 'limited').length
  const unknownCount = checks.filter((check) => check.status === 'unknown').length

  return checks.length === value.checks.length &&
    codes.size === integrityCheckCodes.size &&
    limitedCount === value.limitedCheckCount &&
    unknownCount === value.unknownCheckCount &&
    value.status === (limitedCount > 0 ? 'limited' : 'complete')
}

const isIntegrityCheck = (value: unknown): value is IRecordingComparisonIntegrityCheck =>
  isRecord(value) &&
  typeof value.code === 'string' &&
  integrityCheckCodes.has(value.code as IRecordingComparisonIntegrityCheck['code']) &&
  typeof value.status === 'string' &&
  integrityCheckStatuses.has(value.status as IRecordingComparisonIntegrityCheck['status']) &&
  typeof value.label === 'string' &&
  value.label.trim().length > 0 &&
  typeof value.detail === 'string' &&
  value.detail.trim().length > 0

const isAnalysisSpecification = (value: unknown): value is IRecordingComparisonAnalysisSpecification => {
  if (
    !isRecord(value) ||
    value.contractVersion !== 'comparison-analysis-v1' ||
    (value.scope !== 'full_duration' && value.scope !== 'roi') ||
    value.differenceConvention !== 'compare_a_minus_compare_b' ||
    value.aggregateStatistics !== 'mean_median_minimum_maximum_spread' ||
    !Array.isArray(value.metricMethods) ||
    value.metricMethods.length !== expectedMetricMethods.length
  ) {
    return false
  }

  return value.metricMethods.every((method, index) => isMetricMethod(method, expectedMetricMethods[index]))
}

const isMetricMethod = (
  value: unknown,
  expected: Pick<IRecordingComparisonMetricMethod, 'metricKey' | 'unit' | 'methodId'>
): value is IRecordingComparisonMetricMethod =>
  isRecord(value) &&
  value.metricKey === expected.metricKey &&
  value.unit === expected.unit &&
  value.methodId === expected.methodId &&
  value.methodVersion === '1' &&
  typeof value.label === 'string' &&
  value.label.trim().length > 0 &&
  typeof value.definition === 'string' &&
  value.definition.trim().length > 0

const hasMatchingAnalysisScope = (
  specification: IRecordingComparisonAnalysisSpecification,
  regionOfInterest: unknown
) => regionOfInterest === null
  ? specification.scope === 'full_duration'
  : specification.scope === 'roi' && isRegionOfInterest(regionOfInterest)

const isRegionOfInterest = (value: unknown) =>
  isRecord(value) &&
  typeof value.startTimeSeconds === 'number' &&
  Number.isFinite(value.startTimeSeconds) &&
  typeof value.endTimeSeconds === 'number' &&
  Number.isFinite(value.endTimeSeconds) &&
  value.startTimeSeconds >= 0 &&
  value.endTimeSeconds > value.startTimeSeconds

const hasMatchingAggregateMetrics = (
  specification: IRecordingComparisonAnalysisSpecification,
  aggregateMetrics: unknown
) => Array.isArray(aggregateMetrics) &&
  aggregateMetrics.length === specification.metricMethods.length &&
  aggregateMetrics.every((metric, index) =>
    isRecord(metric) &&
    metric.metricKey === specification.metricMethods[index].metricKey &&
    metric.unit === specification.metricMethods[index].unit)

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null

const readComparisonError = async (response: Response) => {
  const fallback = 'Comparison results could not be prepared.'
  const text = await response.text()

  if (!text) {
    return fallback
  }

  try {
    const body = JSON.parse(text)

    if (Array.isArray(body?.errors)) {
      return body.errors
        .map((error: { reason?: string }) => error.reason)
        .filter(Boolean)
        .join('. ') || fallback
    }

    if (Array.isArray(body?.errors?.generalErrors)) {
      return body.errors.generalErrors
        .filter((error: unknown): error is string => typeof error === 'string')
        .join('. ') || fallback
    }

    if (typeof body?.message === 'string') {
      return body.message
    }
  } catch {
    return text
  }

  return fallback
}
