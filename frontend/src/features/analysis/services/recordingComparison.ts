import { API_BASE_URL } from '../../../common/api/config'
import type { IRequestedRegionOfInterest } from '../utils/analysisWorkspaceState'
import type {
  IRecordingComparisonIntegrityAssessment,
  IRecordingComparisonIntegrityCheck,
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
