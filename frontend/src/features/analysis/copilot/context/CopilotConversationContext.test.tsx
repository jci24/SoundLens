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
  const { turns, submit } = useCopilotConversation()
  return (
    <div>
      <span>Turns: {turns.length}</span>
      <button type="button" onClick={() => submit({ question: 'Explain RMS.' })}>Ask</button>
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
})
