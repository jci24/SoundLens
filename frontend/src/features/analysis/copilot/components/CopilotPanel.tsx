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
    <section className="copilot-panel" aria-label="AI investigation copilot">
      <header className="copilot-panel__header">
        <p className="copilot-panel__eyebrow">Analysis</p>
        <h2 className="copilot-panel__title">Copilot</h2>
        <p className="copilot-panel__subtitle">
          Ask a question about the loaded recordings. The AI investigates using real backend measurements — it never invents values.
        </p>
      </header>

      <div className="copilot-panel__body">
        <CopilotInput isLoading={isLoading} onSubmit={handleSubmit} />

        {/* Loading state */}
        {isLoading && (
          <div className="copilot-panel__loading" aria-live="polite">
            <Loader2 className="copilot-panel__spinner" size={18} />
            <span>Investigating — running analysis tools…</span>
          </div>
        )}

        {/* Error state */}
        {!isLoading && error && (
          <div className="copilot-panel__error" role="alert">
            <span>{error}</span>
          </div>
        )}

        {/* Response */}
        {!isLoading && !error && response && (
          <CopilotResponse
            response={response}
            onRegenerate={() => {
              if (response) reset()
            }}
          />
        )}

        {/* Empty state */}
        {!isLoading && !error && !response && (
          <p className="copilot-panel__empty">
            Select signals in the sidebar, then ask a question above.
          </p>
        )}
      </div>
    </section>
  )
}

export { CopilotPanel }
