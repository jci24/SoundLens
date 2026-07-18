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
  it('renders next steps inside a labeled section with tool details toggle', () => {
    render(<CopilotResponse response={response} onRegenerate={() => {}} />)

    expect(screen.getByText('Workspace evidence')).toBeInTheDocument()
    expect(screen.getByText('Suggested next steps')).toBeInTheDocument()
    expect(screen.getByText('Zoom into 1 to 3 kHz.')).toBeInTheDocument()
    expect(screen.getByText('Compare against the ROI spectrum.')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /1 tool used/i }))

    expect(screen.getByText('Spectrum summary')).toBeInTheDocument()
  })

  it('labels general answers without rendering evidence-only sections', () => {
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

    expect(screen.getByText('General knowledge')).toBeInTheDocument()
    expect(screen.queryByText('Evidence used')).not.toBeInTheDocument()
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

    expect(screen.getByText('Web research')).toBeInTheDocument()
    expect(screen.queryByText('Evidence used')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Source: Standards body' })).toHaveAttribute(
      'href',
      'https://example.com/standard',
    )
    screen.getAllByRole('link', { name: /Standards body/ }).forEach((link) => {
      expect(link).toHaveAttribute('target', '_blank')
    })
  })
})
