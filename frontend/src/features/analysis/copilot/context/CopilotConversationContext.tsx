import type { ReactNode } from 'react'
import { useCopilotQuery } from '../hooks/useCopilotQuery'
import { CopilotConversationContext } from './copilotConversationContextValue'

const CopilotConversationProvider = ({ children }: { children: ReactNode }) => {
  const conversation = useCopilotQuery()
  return (
    <CopilotConversationContext.Provider value={conversation}>
      {children}
    </CopilotConversationContext.Provider>
  )
}

export { CopilotConversationProvider }
