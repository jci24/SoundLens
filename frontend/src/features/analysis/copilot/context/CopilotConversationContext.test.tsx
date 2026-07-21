import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { useState } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CopilotConversationProvider } from './CopilotConversationContext'
import { useCopilotConversation } from './useCopilotConversation'

const mockStreamAgentQuery = vi.fn()

vi.mock('../services/copilotService', () => ({
  streamAgentQuery: (...args: unknown[]) => mockStreamAgentQuery(...args),
}))

const ConversationConsumer = () => {
  const { turns, submit, recordActivity } = useCopilotConversation()
  return (
    <div>
      <span>Turns: {turns.length}</span>
      <span>Activity: {turns[0]?.activity.length ?? 0}</span>
      <button type="button" onClick={() => submit({ question: 'Explain RMS.' })}>Ask</button>
      <button
        type="button"
        onClick={() => turns[0] && recordActivity(turns[0].id, {
          sequence: 1,
          kind: 'action',
          status: 'completed',
          title: 'Navigation approved',
          summary: 'Opening Evidence.',
        })}
      >
        Record action
      </button>
    </div>
  )
}

const CloseablePanelHarness = () => {
  const [isOpen, setIsOpen] = useState(true)
  return (
    <>
      <button type="button" onClick={() => setIsOpen((current) => !current)}>Toggle</button>
      {isOpen && <ConversationConsumer />}
    </>
  )
}

describe('CopilotConversationProvider', () => {
  beforeEach(() => {
    mockStreamAgentQuery.mockReset()
    mockStreamAgentQuery.mockResolvedValue({
      answer: 'RMS is an effective signal level.',
      answerMode: 'general',
      citedEvidence: [],
      limitations: [],
      nextSteps: [],
      toolsUsed: [],
    })
  })

  it('retains completed turns while the panel closes and resets when the route provider unmounts', async () => {
    const view = render(
      <CopilotConversationProvider>
        <CloseablePanelHarness />
      </CopilotConversationProvider>
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))
    await waitFor(() => expect(screen.getByText('Turns: 1')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Toggle' }))
    expect(screen.queryByText('Turns: 1')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Toggle' }))
    expect(screen.getByText('Turns: 1')).toBeInTheDocument()

    view.unmount()
    render(
      <CopilotConversationProvider>
        <ConversationConsumer />
      </CopilotConversationProvider>
    )
    expect(screen.getByText('Turns: 0')).toBeInTheDocument()
  })

  it('records a backend navigation approval against the originating turn', async () => {
    render(
      <CopilotConversationProvider>
        <ConversationConsumer />
      </CopilotConversationProvider>
    )

    fireEvent.click(screen.getByRole('button', { name: 'Ask' }))
    await waitFor(() => expect(screen.getByText('Turns: 1')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Record action' }))

    expect(screen.getByText('Activity: 1')).toBeInTheDocument()
  })
})
