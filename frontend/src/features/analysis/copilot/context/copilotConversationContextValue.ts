import { createContext } from 'react'
import type { IUseCopilotQueryResult } from '../hooks/useCopilotQuery'

const CopilotConversationContext = createContext<IUseCopilotQueryResult | null>(null)

export { CopilotConversationContext }
