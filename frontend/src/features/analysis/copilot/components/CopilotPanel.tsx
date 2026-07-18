import { useEffect, useRef } from 'react'
import { Loader2 } from 'lucide-react'
import { CopilotInput } from './CopilotInput'
import { CopilotResponse } from './CopilotResponse'
import { useCopilotQuery } from '../hooks/useCopilotQuery'
import { useAnalysisWorkspaceStore } from '../../stores/useAnalysisWorkspaceStore'
import type { IAnalysisRegionOfInterest, ITimeWaveformRecording } from '../../types'
import './CopilotPanel.scss'

interface ICopilotPanelProps {
  selectedSignalIds: string[]
  regionOfInterest: IAnalysisRegionOfInterest | null
  recordings: ITimeWaveformRecording[]
}

const CopilotPanel = ({ selectedSignalIds, regionOfInterest, recordings }: ICopilotPanelProps) => {
  const { turns, isLoading, submit, retry } = useCopilotQuery()
  const threadRef = useRef<HTMLDivElement | null>(null)
  const hasConversation = turns.length > 0
  const comparisonContext = useAnalysisWorkspaceStore((state) => state.comparisonCopilotContext)
  const recordingGroupAssignments = useAnalysisWorkspaceStore((state) => state.recordingGroupAssignments)

  useEffect(() => {
    if (threadRef.current) {
      threadRef.current.scrollTop = threadRef.current.scrollHeight
    }
  }, [turns, isLoading])

  const handleSubmit = (question: string) => {
    const mentionMatches = Array.from(question.matchAll(/@\[([^\]]+)\]\(([^)]+)\)/g))
    const mentionedIds = mentionMatches.map((m) => m[2])
    const contextMode = mentionedIds.length > 0 ? 'workspace' : 'auto'
    const activeComparisonContext = mentionedIds.length === 0
      ? comparisonContext ?? undefined
      : undefined
    const recordingA = recordings.filter((recording) => recordingGroupAssignments[recording.recordingId] === 'A')
    const recordingB = recordings.filter((recording) => recordingGroupAssignments[recording.recordingId] === 'B')
    const comparisonPair = mentionedIds.length === 0 && !activeComparisonContext &&
      recordingA.length === 1 && recordingB.length === 1
      ? { recordingIdA: recordingA[0].recordingId, recordingIdB: recordingB[0].recordingId }
      : undefined
    const resolvedIds = mentionedIds.length > 0
      ? mentionedIds
      : activeComparisonContext
        ? [activeComparisonContext.signalIdA, activeComparisonContext.signalIdB]
        : selectedSignalIds.length > 0 ? selectedSignalIds : undefined
    const cleanQuestion = question.replace(/@\[([^\]]+)\]\([^)]+\)/g, '@$1')
    submit({
      question: cleanQuestion,
      contextMode,
      signalIds: resolvedIds,
      startTimeSeconds: regionOfInterest?.startTimeSeconds,
      endTimeSeconds: regionOfInterest?.endTimeSeconds,
      comparisonContext: activeComparisonContext,
      comparisonPair,
    })
  }

  return (
    <section className="copilot-panel" aria-label="AI copilot">
      <div className="copilot-panel__thread" ref={threadRef}>
        {!hasConversation && (
          <div className="copilot-panel__empty-state">
            <p className="copilot-panel__empty-label">Copilot</p>
            <p className="copilot-panel__empty-hint">Ask about your evidence or a general technical question.</p>
          </div>
        )}

        {hasConversation && (
          <>
            {turns.map((turn) => (
              <div className="copilot-panel__turn" key={turn.id}>
                <div className="copilot-panel__user-bubble">
                  <span>{turn.question}</span>
                </div>

                {turn.isLoading && (
                  <div className="copilot-panel__status-marker" aria-live="polite" role="status">
                    <Loader2 className="copilot-panel__spinner" size={13} />
                    <span>Thinking…</span>
                  </div>
                )}

                {!turn.isLoading && turn.error && (
                  <div className="copilot-panel__status-marker copilot-panel__status-marker--error" role="alert">
                    <span>{turn.error}</span>
                  </div>
                )}

                {!turn.isLoading && !turn.error && turn.response && (
                  <div className="copilot-panel__assistant-response">
                    <CopilotResponse response={turn.response} onRegenerate={() => retry(turn.id)} />
                  </div>
                )}
              </div>
            ))}
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
