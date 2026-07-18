import { useId, useState } from 'react'
import { Check, ChevronDown, ChevronUp, Loader2, OctagonX } from 'lucide-react'
import type { IAgentActivityEvent } from '../types/copilot.types'
import './CopilotActivityTrace.scss'

interface ICopilotActivityTraceProps {
  activity: IAgentActivityEvent[]
  isRunning: boolean
  isStopped: boolean
}

const CopilotActivityTrace = ({ activity, isRunning, isStopped }: ICopilotActivityTraceProps) => {
  const [isOpen, setIsOpen] = useState(false)
  const detailsId = useId()
  if (activity.length === 0) return null

  const currentStep = [...activity].reverse().find((step) => step.status === 'running') ?? activity.at(-1)
  const summary = isRunning
    ? currentStep?.title ?? 'Preparing response'
    : isStopped
      ? `Stopped after ${activity.length} step${activity.length === 1 ? '' : 's'}`
      : `Prepared with ${activity.length} step${activity.length === 1 ? '' : 's'}`

  return (
    <section className="copilot-activity" aria-label="Investigation activity">
      <div className="copilot-activity__current" aria-live="polite" role="status">
        {isRunning ? <Loader2 className="copilot-activity__spinner" size={12} /> : isStopped ? <OctagonX size={12} /> : <Check size={12} />}
        <span>{summary}{isRunning ? '…' : ''}</span>
      </div>
      <button
        className="copilot-activity__toggle"
        type="button"
        aria-expanded={isOpen}
        aria-controls={detailsId}
        onClick={() => setIsOpen((open) => !open)}
      >
        <span>{isOpen ? 'Hide activity' : 'View activity'}</span>
        {isOpen ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
      </button>
      {isOpen && (
        <ol className="copilot-activity__steps" id={detailsId}>
          {activity.map((step) => (
            <li className={`copilot-activity__step copilot-activity__step--${step.status}`} key={step.sequence}>
              <span className="copilot-activity__marker" aria-hidden="true" />
              <div>
                <p>{step.title}</p>
                <span>{step.summary}</span>
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}

export { CopilotActivityTrace }
