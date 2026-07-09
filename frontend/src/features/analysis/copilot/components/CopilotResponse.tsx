import { useState } from 'react'
import { AlertCircle, ChevronDown, ChevronUp, RefreshCw } from 'lucide-react'
import { CopilotEvidenceBadge } from './CopilotEvidenceBadge'
import type { IAgentQueryResponse } from '../types/copilot.types'
import './CopilotResponse.scss'

const TOOL_DISPLAY_NAMES: Record<string, string> = {
  get_signal_metrics: 'Signal metrics',
  get_signal_findings: 'Signal findings',
  get_spectrum_summary: 'Spectrum summary',
  compare_signals: 'Compare signals',
}

interface ICopilotResponseProps {
  response: IAgentQueryResponse
  onRegenerate: () => void
}

const CopilotResponse = ({ response, onRegenerate }: ICopilotResponseProps) => {
  const [isToolsOpen, setIsToolsOpen] = useState(false)

  return (
    <div className="copilot-response">
      <p className="copilot-response__answer">{response.answer}</p>

      {response.citedEvidence.length > 0 && (
        <section className="copilot-response__section" aria-label="Evidence used">
          <p className="copilot-response__section-label">Evidence used</p>
          <div className="copilot-response__evidence-badges">
            {response.citedEvidence.map((item, index) => (
              <CopilotEvidenceBadge key={index} item={item} />
            ))}
          </div>
        </section>
      )}

      {response.limitations.length > 0 && (
        <section className="copilot-response__section" aria-label="Limitations">
          <div className="copilot-response__limitations">
            <AlertCircle size={11} />
            <span>{response.limitations.join(' · ')}</span>
          </div>
        </section>
      )}

      {response.nextSteps.length > 0 && (
        <section className="copilot-response__section" aria-label="Suggested next steps">
          <p className="copilot-response__section-label">Suggested next steps</p>
          <ul className="copilot-response__next-steps-list">
            {response.nextSteps.map((step, index) => (
              <li className="copilot-response__next-step-item" key={index}>
                <span className="copilot-response__next-step-bullet" aria-hidden="true">•</span>
                <span>{step}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="copilot-response__footer">
        {response.toolsUsed.length > 0 && (
          <button
            className="copilot-response__tools-toggle"
            type="button"
            onClick={() => setIsToolsOpen((prev) => !prev)}
            aria-expanded={isToolsOpen}
          >
            {isToolsOpen ? <ChevronUp size={11} /> : <ChevronDown size={11} />}
            <span>{response.toolsUsed.length} tool{response.toolsUsed.length !== 1 ? 's' : ''} used</span>
          </button>
        )}
        <button
          className="copilot-response__regenerate"
          type="button"
          onClick={onRegenerate}
          aria-label="Re-run"
        >
          <RefreshCw size={11} />
          <span>Re-run</span>
        </button>
      </div>

      {isToolsOpen && (
        <ul className="copilot-response__tools-list">
          {response.toolsUsed.map((tool) => (
            <li key={tool}>{TOOL_DISPLAY_NAMES[tool] ?? tool}</li>
          ))}
        </ul>
      )}
    </div>
  )
}

export { CopilotResponse }
