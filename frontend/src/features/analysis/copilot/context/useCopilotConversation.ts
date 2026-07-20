import { useContext } from 'react'
import type { IUseCopilotQueryResult } from '../hooks/useCopilotQuery'
import { CopilotConversationContext } from './copilotConversationContextValue'

const useCopilotConversation = (): IUseCopilotQueryResult => {
  const conversation = useContext(CopilotConversationContext)
  if (!conversation) {
    throw new Error('useCopilotConversation must be used inside CopilotConversationProvider.')
  }
  return conversation
}

export { useCopilotConversation }
