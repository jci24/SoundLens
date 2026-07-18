import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { CopilotActivityTrace } from './CopilotActivityTrace'

const activity = [
  {
    sequence: 1,
    kind: 'routing' as const,
    status: 'completed' as const,
    title: 'Understanding your question',
    summary: 'The request depends on the selected comparison.',
  },
  {
    sequence: 2,
    kind: 'evidence_check' as const,
    status: 'running' as const,
    title: 'Checking selected evidence',
    summary: 'Reconstructing backend evidence.',
  },
]

describe('CopilotActivityTrace', () => {
  it('shows only the current status until the accessible disclosure opens', () => {
    render(<CopilotActivityTrace activity={activity} isRunning isStopped={false} />)

    expect(screen.getByRole('status')).toHaveTextContent('Checking selected evidence…')
    const toggle = screen.getByRole('button', { name: 'How this answer was prepared' })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByText('The request depends on the selected comparison.')).not.toBeInTheDocument()

    fireEvent.click(toggle)
    expect(screen.getByRole('button', { name: 'Hide preparation details' })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('The request depends on the selected comparison.')).toBeVisible()
    expect(screen.getByRole('region', { name: 'How this answer was prepared' })).toBeVisible()
  })

  it('uses completed and stopped summaries without reasoning language', () => {
    const { rerender } = render(
      <CopilotActivityTrace activity={activity} isRunning={false} isStopped={false} />
    )
    expect(screen.getByRole('status')).toHaveTextContent('Prepared with 2 steps')

    rerender(<CopilotActivityTrace activity={activity} isRunning={false} isStopped />)
    expect(screen.getByRole('status')).toHaveTextContent('Stopped after 2 steps')
    expect(screen.queryByText(/reasoning|thought/i)).not.toBeInTheDocument()
  })

  it('renders nothing for simple turns without activity', () => {
    const { container } = render(
      <CopilotActivityTrace activity={[]} isRunning={false} isStopped={false} />
    )
    expect(container).toBeEmptyDOMElement()
  })
})
