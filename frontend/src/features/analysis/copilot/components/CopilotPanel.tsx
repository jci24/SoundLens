import { Loader2 } from 'lucide-react'
import { CopilotInput } from './CopilotInput'
import { CopilotResponse } from './CopilotResponse'
import { useCopilotQuery } from '../hooks/useCopilotQuery'
import type { IAnalysisRegionOfInterest } from '../../types'
import './CopilotPanel.scss'

interface ICopilotPanelProps {
  selectedSignalIds: string[]
  regionOfInterest: IAnalysisRegionOfInterest | null
}

const CopilotPanel = ({ selectedSignalIds, regionOfInterest }: ICopilotPanelProps) => {
  const { response, isLoading, error, submit, reset } = useCopilotQuery()
  const hasContent = isLoading || !!error || !!response

  const handleSubmit = (question: string) => {
    reset()
    submit({
      question,
      signalIds: selectedSignalIds.length > 0 ? selectedSignalIds : undefined,
      startTimeSeconds: regionOfInterest?.startTimeSeconds,
      endTimeSeconds: regionOfInterest?.endTimeSeconds,
    })
  }

  return (
    <section className="copilot-panel" aria-label="AI copilot">
      {/* Thread area — fills available space, scrollable */}
      <div className="copilot-panel__thread">
        {!hasContent && (
          <div className="copilot-panel__empty-state">
            <p className="copilot-panel__empty-label">Copilot</p>
            <p className="copilot-panel__empty-hint">Ask a question about the loaded recordings.</p>
          </div>
        )}

        {isLoading && (
          <div className="copilot-panel__loading" aria-live="polite">
            <Loader2 className="copilot-panel__spinner" size={14} />
            <span>Investigating…</span>
          </div>
        )}

        {!isLoading && error && (
          <div className="copilot-panel__error" role="alert">
            <span>{error}</span>
          </div>
        )}

        {!isLoading && !error && response && (
          <CopilotResponse
            response={response}
            onRegenerate={reset}
          />
        )}
      </div>

      {/* Input bar — always pinned to bottom */}
      <div className="copilot-panel__input-bar">
        <CopilotInput
          isLoading={isLoading}
          showSuggestions={!hasContent}
          onSubmit={handleSubmit}
        />
      </div>
    </section>
  )
}

export { CopilotPanel }
