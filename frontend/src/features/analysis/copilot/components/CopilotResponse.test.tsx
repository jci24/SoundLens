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

describe('CopilotResponse', () => {
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
