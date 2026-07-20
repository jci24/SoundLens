import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { CopilotResponse } from './CopilotResponse'

const response = {
  answer: 'The strongest tonal peak is around 2 kHz.',
  answerMode: 'workspace' as const,
  citedEvidence: [
    {
      toolName: 'get_spectrum_summary',
      signalId: 'signal-1',
      summary: 'Peak around 2 kHz.',
    },
  ],
  limitations: ['This is uncalibrated evidence.'],
  nextSteps: ['Zoom into 1 to 3 kHz.', 'Compare against the ROI spectrum.'],
  toolsUsed: ['get_spectrum_summary'],
}

const structuredObservations = [
  {
    observationId: 'obs_v1_1234567890abcdef12345678',
    kind: 'comparison_metric' as const,
    status: 'limited' as const,
    scope: { kind: 'roi' as const, startTimeSeconds: 0.25, endTimeSeconds: 0.75 },
    limitationCodes: ['LowCoverage'],
    evidenceReferences: [
      {
        referenceId: 'obs_v1_1234567890abcdef12345678',
        evidenceType: 'comparison_metric' as const,
        recordingIds: ['recording-a', 'recording-b'],
        signalIds: ['recording-a:ch:0', 'recording-b:ch:0'],
        metricKey: 'rmsAmplitudeDelta',
        scope: { kind: 'roi' as const, startTimeSeconds: 0.25, endTimeSeconds: 0.75 },
      },
    ],
    comparisonMetric: {
      metricKey: 'rmsAmplitudeDelta' as const,
      metricLabel: 'RMS amplitude',
      unit: 'FS' as const,
      aggregate: {
        comparedPairCount: 1,
        missingValueCount: 0,
        meanDifference: -0.25,
        medianDifference: -0.25,
        minimumDifference: -0.25,
        maximumDifference: -0.25,
        spread: 0,
      },
      selectedPair: {
        recordingIdA: 'recording-a',
        recordingFileNameA: 'baseline.wav',
        signalIdA: 'recording-a:ch:0',
        signalDisplayNameA: 'Channel 1',
        valueA: 0.25,
        recordingIdB: 'recording-b',
        recordingFileNameB: 'candidate.wav',
        signalIdB: 'recording-b:ch:0',
        signalDisplayNameB: 'Channel 1',
        valueB: 0.5,
        difference: -0.25,
      },
    },
    signalFinding: null,
  },
  {
    observationId: 'obs_v1_abcdef1234567890abcdef12',
    kind: 'signal_finding' as const,
    status: 'complete' as const,
    scope: { kind: 'roi' as const, startTimeSeconds: 0.25, endTimeSeconds: 0.75 },
    limitationCodes: [],
    evidenceReferences: [
      {
        referenceId: 'obs_v1_abcdef1234567890abcdef12',
        evidenceType: 'signal_finding' as const,
        recordingIds: ['recording-a'],
        signalIds: ['recording-a:ch:0'],
        metricKey: null,
        scope: { kind: 'roi' as const, startTimeSeconds: 0.25, endTimeSeconds: 0.75 },
      },
    ],
    comparisonMetric: null,
    signalFinding: {
      side: 'A' as const,
      recordingId: 'recording-a',
      recordingFileName: 'baseline.wav',
      signalId: 'recording-a:ch:0',
      signalDisplayName: 'Channel 1',
      category: 'TonalPeak',
      severity: 'Info',
      label: 'Dominant tonal component',
      detail: 'Peak near 1 kHz',
    },
  },
]

const investigationPlan = {
  planId: 'plan_v1_1234567890abcdef12345678',
  version: '1' as const,
  status: 'preview' as const,
  objective: 'Compare the recordings using complementary evidence.',
  scope: { kind: 'full_duration' as const, startTimeSeconds: null, endTimeSeconds: null },
  steps: [
    {
      stepId: 'step-1',
      order: 1,
      title: 'Inspect waveform evidence',
      purpose: 'Review event shape and timing.',
      capabilityId: 'waveform',
      capabilityLabel: 'Waveform inspection',
      category: 'analysis' as const,
      dependsOnStepIds: [],
      parameterKeys: ['scope', 'signals'],
      requiredEvidence: ['imported_recordings'],
      completionCriteria: ['Waveform evidence is available for review.'],
      costClass: 'interactive' as const,
      requiresApproval: false,
    },
    {
      stepId: 'step-2',
      order: 2,
      title: 'Select a focused region',
      purpose: 'Narrow the review to the event relevant to the decision.',
      capabilityId: 'roi',
      capabilityLabel: 'Region selection',
      category: 'inspection' as const,
      dependsOnStepIds: ['step-1'],
      parameterKeys: ['scope'],
      requiredEvidence: ['imported_recordings'],
      completionCriteria: ['A review scope is available.'],
      costClass: 'interactive' as const,
      requiresApproval: true,
    },
  ],
}

describe('CopilotResponse', () => {
  it('keeps a validated investigation preview collapsed without execution controls', () => {
    render(
      <CopilotResponse
        response={{ ...response, investigationPlan }}
        onRegenerate={() => {}}
      />
    )

    const toggle = screen.getByRole('button', { name: /Investigation plan 2 steps · Preview/i })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByText('Inspect waveform evidence')).not.toBeInTheDocument()

    fireEvent.click(toggle)

    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText(investigationPlan.objective)).toBeVisible()
    expect(screen.getByText('Waveform inspection')).toBeVisible()
    expect(screen.getByText('Required before execution')).toBeVisible()
    expect(screen.getByText('step-1')).toBeVisible()
    expect(screen.getByTitle(investigationPlan.planId)).toHaveTextContent('plan_v1_12345678…')
    expect(screen.queryByRole('button', { name: /run plan|approve plan|execute plan/i })).not.toBeInTheDocument()
  })

  it('keeps backend-owned measured observations collapsed until requested', () => {
    render(
      <CopilotResponse
        response={{ ...response, structuredObservations }}
        onRegenerate={() => {}}
      />
    )

    const toggle = screen.getByRole('button', { name: /Measured evidence 2 observations/i })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByText('RMS amplitude')).not.toBeInTheDocument()

    fireEvent.click(toggle)

    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('RMS amplitude')).toBeVisible()
    expect(screen.getByText('0.25–0.75 s')).toBeVisible()
    expect(screen.getByText('A 0.25 FS · B 0.5 FS · Δ -0.25 FS')).toBeVisible()
    expect(screen.getByText('Dominant tonal component')).toBeVisible()
    expect(screen.getByText('Limits: LowCoverage')).toBeVisible()
    expect(screen.getByTitle('obs_v1_1234567890abcdef12345678')).toHaveTextContent('obs_v1_12345678…')
  })

  it.each([
    ['supported', 'Evidence supported'],
    ['partial', 'Partial evidence'],
    ['missing', 'Evidence missing'],
    ['contradicted', 'Evidence conflicts'],
    ['unavailable', 'Evidence unavailable'],
  ] as const)('renders the %s backend sufficiency status without another card', (status, label) => {
    render(
      <CopilotResponse
        response={{
          ...response,
          evidenceSufficiency: {
            intent: 'digital_level_difference',
            status,
            label,
            reason: 'Backend-owned reason.',
            requiredEvidence: ['Aligned observations'],
            availableEvidence: ['Selected pair'],
            limitationCodes: [],
          },
        }}
        onRegenerate={() => {}}
      />
    )

    const sufficiency = screen.getByRole('region', { name: 'Evidence sufficiency' })
    expect(sufficiency).toHaveAttribute('data-status', status)
    expect(sufficiency).toHaveTextContent(`${label}Backend-owned reason.`)
  })

  it('renders next steps inside a labeled section with tool details toggle', () => {
    render(<CopilotResponse response={response} onRegenerate={() => {}} />)

    expect(screen.queryByText('Workspace evidence')).not.toBeInTheDocument()
    expect(screen.getByText('Suggested next steps')).toBeInTheDocument()
    expect(screen.getByText('Zoom into 1 to 3 kHz.')).toBeInTheDocument()
    expect(screen.getByText('Compare against the ROI spectrum.')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /1 tool used/i }))

    expect(screen.getByText('Spectrum summary')).toBeInTheDocument()
  })

  it('does not duplicate tool disclosure when activity already presents the tools', () => {
    render(<CopilotResponse response={response} hasActivityTrace onRegenerate={() => {}} />)

    expect(screen.queryByRole('button', { name: /tool used/i })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Re-run' })).toBeVisible()
  })

  it('renders general answers without exposing the internal answer mode', () => {
    render(
      <CopilotResponse
        response={{
          answer: 'A Fourier transform decomposes a signal into frequency components.',
          answerMode: 'general',
          citedEvidence: [],
          limitations: [],
          nextSteps: [],
          toolsUsed: [],
        }}
        onRegenerate={() => {}}
      />
    )

    expect(screen.queryByText('General knowledge')).not.toBeInTheDocument()
    expect(screen.queryByText('Evidence used')).not.toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Evidence sufficiency' })).not.toBeInTheDocument()
  })

  it('renders web answers with claim-adjacent and listed clickable citations', () => {
    render(
      <CopilotResponse
        response={{
          answer: 'The current standard was updated in 2026.',
          answerMode: 'web',
          citedEvidence: [],
          externalCitations: [
            {
              title: 'Standards body',
              url: 'https://example.com/standard',
              startIndex: 4,
              endIndex: 27,
            },
          ],
          limitations: [],
          nextSteps: [],
          toolsUsed: ['web_search'],
        }}
        onRegenerate={() => {}}
      />
    )

    expect(screen.queryByText('Web research')).not.toBeInTheDocument()
    expect(screen.queryByText('Evidence used')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Source: Standards body' })).toHaveAttribute(
      'href',
      'https://example.com/standard',
    )
    screen.getAllByRole('link', { name: /Standards body/ }).forEach((link) => {
      expect(link).toHaveAttribute('target', '_blank')
    })
  })

  it('renders adaptive investigation guidance without exposing the internal answer mode', () => {
    render(
      <CopilotResponse
        response={{
          answer: 'Clarify which engineering decision this comparison must support.',
          answerMode: 'guidance',
          citedEvidence: [],
          limitations: [],
          nextSteps: ['Define the decision criterion.'],
          toolsUsed: [],
        }}
        onRegenerate={() => {}}
      />
    )

    expect(screen.queryByText('Investigation guidance')).not.toBeInTheDocument()
    expect(screen.queryByText('Evidence used')).not.toBeInTheDocument()
  })
})
