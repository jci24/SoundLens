import { useEffect, useRef } from 'react'
import { Loader2 } from 'lucide-react'
import { CopilotInput } from './CopilotInput'
import { CopilotResponse } from './CopilotResponse'
import { useCopilotQuery } from '../hooks/useCopilotQuery'
import type { IAnalysisRegionOfInterest, ITimeWaveformRecording } from '../../types'
import './CopilotPanel.scss'

interface ICopilotPanelProps {
  selectedSignalIds: string[]
  regionOfInterest: IAnalysisRegionOfInterest | null
  recordings: ITimeWaveformRecording[]
}

const CopilotPanel = ({ selectedSignalIds, regionOfInterest, recordings }: ICopilotPanelProps) => {
  const { response, lastQuestion, isLoading, error, submit, reset } = useCopilotQuery()
  const threadRef = useRef<HTMLDivElement | null>(null)
  const hasConversation = !!lastQuestion

  useEffect(() => {
    if (threadRef.current) {
      threadRef.current.scrollTop = threadRef.current.scrollHeight
    }
  }, [lastQuestion, response, isLoading])

  const handleSubmit = (question: string) => {
    reset()
    const mentionMatches = Array.from(question.matchAll(/@\[([^\]]+)\]\(([^)]+)\)/g))
    const mentionedIds = mentionMatches.map((m) => m[2])
    const resolvedIds = mentionedIds.length > 0
      ? mentionedIds
      : selectedSignalIds.length > 0 ? selectedSignalIds : undefined
    const cleanQuestion = question.replace(/@\[([^\]]+)\]\([^)]+\)/g, '@$1')
    submit({
      question: cleanQuestion,
      signalIds: resolvedIds,
      startTimeSeconds: regionOfInterest?.startTimeSeconds,
      endTimeSeconds: regionOfInterest?.endTimeSeconds,
    })
  }

  return (
    <section className="copilot-panel" aria-label="AI copilot">
      <div className="copilot-panel__thread" ref={threadRef}>
        {!hasConversation && (
          <div className="copilot-panel__empty-state">
            <p className="copilot-panel__empty-label">Copilot</p>
            <p className="copilot-panel__empty-hint">Ask a question about the loaded recordings.</p>
          </div>
        )}

        {hasConversation && (
          <>
            <div className="copilot-panel__user-bubble">
              <span>{lastQuestion.replace(/@\[([^\]]+)\]\([^)]+\)/g, '@$1')}</span>
            </div>

            {isLoading && (
              <div className="copilot-panel__thinking" aria-live="polite">
                <Loader2 className="copilot-panel__spinner" size={13} />
                <span>Thinking…</span>
              </div>
            )}

            {!isLoading && error && (
              <div className="copilot-panel__error" role="alert">
                <span>{error}</span>
              </div>
            )}

            {!isLoading && !error && response && (
              <CopilotResponse response={response} onRegenerate={reset} />
            )}
          </>
        )}
      </div>

      <div className="copilot-panel__input-bar">
        <CopilotInput
          isLoading={isLoading}
          recordings={recordings}
          showSuggestions={!hasConversation}
          onSubmit={handleSubmit}
        />
      </div>
    </section>
  )
}

export { CopilotPanel }
