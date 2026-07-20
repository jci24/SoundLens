import { describe, expect, it } from 'vitest'
import { isInvestigationPlan } from './investigationPlanValidation'

const buildPlan = () => ({
  planId: 'plan_v1_1234567890abcdef12345678',
  version: '1',
  status: 'preview',
  objective: 'Compare the recordings using complementary evidence.',
  scope: { kind: 'full_duration', startTimeSeconds: null, endTimeSeconds: null },
  steps: [
    {
      stepId: 'step-1',
      order: 1,
      title: 'Inspect waveform evidence',
      purpose: 'Review event shape and timing.',
      capabilityId: 'waveform',
      capabilityLabel: 'Waveform inspection',
      category: 'analysis',
      dependsOnStepIds: [],
      parameterKeys: ['scope', 'signals'],
      requiredEvidence: ['imported_recordings'],
      completionCriteria: ['Waveform evidence is available for review.'],
      costClass: 'interactive',
      requiresApproval: false,
    },
    {
      stepId: 'step-2',
      order: 2,
      title: 'Inspect spectrum evidence',
      purpose: 'Review tonal and broadband differences.',
      capabilityId: 'spectrum',
      capabilityLabel: 'Spectrum inspection',
      category: 'analysis',
      dependsOnStepIds: ['step-1'],
      parameterKeys: ['scope', 'signals'],
      requiredEvidence: ['imported_recordings'],
      completionCriteria: ['Spectrum evidence is available for review.'],
      costClass: 'interactive',
      requiresApproval: false,
    },
  ],
})

describe('isInvestigationPlan', () => {
  it('accepts an absent or valid preview plan', () => {
    expect(isInvestigationPlan(undefined)).toBe(true)
    expect(isInvestigationPlan(null)).toBe(true)
    expect(isInvestigationPlan(buildPlan())).toBe(true)
  })

  it('rejects unknown properties, statuses, categories, costs, and malformed scope', () => {
    const plan = buildPlan()
    expect(isInvestigationPlan({ ...plan, executable: true })).toBe(false)
    expect(isInvestigationPlan({ ...plan, status: 'running' })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      steps: [{ ...plan.steps[0], category: 'mutation' }],
    })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      steps: [{ ...plan.steps[0], costClass: 'unbounded' }],
    })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      scope: { kind: 'roi', startTimeSeconds: 2, endTimeSeconds: 1 },
    })).toBe(false)
  })

  it('rejects duplicate, forward, self, or missing dependencies and invalid ordering', () => {
    const plan = buildPlan()
    expect(isInvestigationPlan({
      ...plan,
      steps: [{ ...plan.steps[0], dependsOnStepIds: ['step-2'] }, plan.steps[1]],
    })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      steps: [{ ...plan.steps[0], dependsOnStepIds: ['step-1'] }, plan.steps[1]],
    })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      steps: [plan.steps[0], { ...plan.steps[1], dependsOnStepIds: ['step-1', 'step-1'] }],
    })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      steps: [plan.steps[1], plan.steps[0]],
    })).toBe(false)
  })

  it('rejects measured-result prose, duplicate policy keys, and malformed IDs', () => {
    const plan = buildPlan()
    expect(isInvestigationPlan({ ...plan, planId: 'plan-v1-unsafe' })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      steps: [{ ...plan.steps[0], purpose: 'The peak is 42 dBFS.' }],
    })).toBe(false)
    expect(isInvestigationPlan({
      ...plan,
      steps: [{ ...plan.steps[0], parameterKeys: ['scope', 'scope'] }],
    })).toBe(false)
  })
})
