import type {
  IAgentInvestigationPlan,
  IAgentInvestigationPlanScope,
} from '../types/copilot.types'

const PLAN_ID_PATTERN = /^plan_v1_[0-9a-f]{24}$/
const STEP_ID_PATTERN = /^step-[1-6]$/
const CATEGORIES = ['analysis', 'inspection', 'audition', 'artifact']
const COST_CLASSES = ['interactive', 'bounded']
const MEASURED_RESULT_PATTERN = /[-+]?\d+(?:\.\d+)?\s*(?:dB(?:FS|\s*SPL)?|FS|Hz|kHz|samples?|ratio|%)\b/i
const PLAN_KEYS = ['planId', 'version', 'status', 'objective', 'scope', 'steps']
const SCOPE_KEYS = ['kind', 'startTimeSeconds', 'endTimeSeconds']
const STEP_KEYS = [
  'stepId', 'order', 'title', 'purpose', 'capabilityId', 'capabilityLabel', 'category',
  'dependsOnStepIds', 'parameterKeys', 'requiredEvidence', 'completionCriteria',
  'costClass', 'requiresApproval',
]

export const isInvestigationPlan = (value: unknown): value is IAgentInvestigationPlan => {
  if (value == null) return true
  if (!isRecord(value) || !hasExactKeys(value, PLAN_KEYS) ||
      typeof value.planId !== 'string' || !PLAN_ID_PATTERN.test(value.planId) ||
      value.version !== '1' || value.status !== 'preview' ||
      !isSafeText(value.objective) || !isPlanScope(value.scope) ||
      !Array.isArray(value.steps) || value.steps.length < 1 || value.steps.length > 6) {
    return false
  }

  const previousStepIds = new Set<string>()
  for (let index = 0; index < value.steps.length; index += 1) {
    const step = value.steps[index]
    if (!isRecord(step) || !hasExactKeys(step, STEP_KEYS) ||
        typeof step.stepId !== 'string' ||
        step.stepId !== `step-${index + 1}` || !STEP_ID_PATTERN.test(step.stepId) ||
        step.order !== index + 1 ||
        !isSafeText(step.title) || !isSafeText(step.purpose) ||
        !isNonEmptyString(step.capabilityId) || !isNonEmptyString(step.capabilityLabel) ||
        !CATEGORIES.includes(String(step.category)) ||
        !isStringArray(step.dependsOnStepIds) ||
        new Set(step.dependsOnStepIds).size !== step.dependsOnStepIds.length ||
        step.dependsOnStepIds.some((dependency) => !previousStepIds.has(dependency)) ||
        !isUniqueStringArray(step.parameterKeys) || !isUniqueStringArray(step.requiredEvidence) ||
        !isStringArray(step.completionCriteria) || step.completionCriteria.length === 0 ||
        step.completionCriteria.some((criterion) => !isSafeText(criterion)) ||
        !COST_CLASSES.includes(String(step.costClass)) ||
        typeof step.requiresApproval !== 'boolean') {
      return false
    }
    previousStepIds.add(step.stepId)
  }

  return true
}

const isPlanScope = (value: unknown): value is IAgentInvestigationPlanScope => {
  if (!isRecord(value) || !hasExactKeys(value, SCOPE_KEYS)) return false
  if (value.kind === 'full_duration') {
    return value.startTimeSeconds === null && value.endTimeSeconds === null
  }
  return value.kind === 'roi' &&
    isFiniteNumber(value.startTimeSeconds) && isFiniteNumber(value.endTimeSeconds) &&
    value.startTimeSeconds >= 0 && value.endTimeSeconds > value.startTimeSeconds
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  Boolean(value) && typeof value === 'object'

const isFiniteNumber = (value: unknown): value is number =>
  typeof value === 'number' && Number.isFinite(value)

const isNonEmptyString = (value: unknown): value is string =>
  typeof value === 'string' && value.trim().length > 0

const isSafeText = (value: unknown): value is string =>
  isNonEmptyString(value) && !MEASURED_RESULT_PATTERN.test(value)

const isStringArray = (value: unknown): value is string[] =>
  Array.isArray(value) && value.every(isNonEmptyString)

const isUniqueStringArray = (value: unknown): value is string[] =>
  isStringArray(value) && new Set(value).size === value.length

const hasExactKeys = (value: Record<string, unknown>, expected: string[]) => {
  const keys = Object.keys(value)
  return keys.length === expected.length && keys.every((key) => expected.includes(key))
}
