import { useState } from 'react'
import { ArrowRight, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { IAgentSuggestedAction } from '../types/copilot.types'
import './CopilotSuggestedActions.scss'

interface ICopilotSuggestedActionsProps {
  actions: IAgentSuggestedAction[]
  canOpenWorkspace: boolean
  onApprove: (action: IAgentSuggestedAction) => Promise<void>
}

const CopilotSuggestedActions = ({ actions, canOpenWorkspace, onApprove }: ICopilotSuggestedActionsProps) => {
  const [activeActionId, setActiveActionId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (actions.length === 0) return null

  const handleApprove = async (action: IAgentSuggestedAction) => {
    const needsRecordings = action.targetRoute !== 'import'
    if (needsRecordings && !canOpenWorkspace) {
      setError('Import recordings before opening this workspace.')
      return
    }

    setActiveActionId(action.actionId)
    setError(null)
    try {
      await onApprove(action)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The navigation action could not be completed.')
      setActiveActionId(null)
    }
  }

  return (
    <section className="copilot-suggested-actions" aria-label="Suggested action">
      {actions.map((action) => (
        <Button
          key={action.actionId}
          disabled={activeActionId !== null}
          size="sm"
          type="button"
          variant="outline"
          onClick={() => void handleApprove(action)}
        >
          {activeActionId === action.actionId
            ? <Loader2 aria-hidden="true" className="copilot-suggested-actions__spinner" />
            : <ArrowRight aria-hidden="true" />}
          {activeActionId === action.actionId ? 'Opening…' : action.label}
        </Button>
      ))}
      {error && <p className="copilot-suggested-actions__error" role="alert">{error}</p>}
    </section>
  )
}

export { CopilotSuggestedActions }
