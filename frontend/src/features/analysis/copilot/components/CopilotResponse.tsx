import { useState } from 'react'
import { AlertCircle, ChevronDown, ChevronUp, RefreshCw, Sparkles } from 'lucide-react'
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
      {/* Disclosure pattern — clearly mark AI-generated content */}
      <div className="copilot-response__disclosure">
        <Sparkles size={12} />
        <span>AI investigation · based on measured evidence only</span>
      </div>

      {/* Answer */}
      <p className="copilot-response__answer">{response.answer}</p>

      {/* Citations / Footprints pattern — show which tools produced the evidence cited */}
      {response.citedEvidence.length > 0 && (
        <div className="copilot-response__evidence-section">
          <span className="copilot-response__section-label">Evidence cited</span>
          <div className="copilot-response__evidence-badges">
            {response.citedEvidence.map((item, index) => (
              <CopilotEvidenceBadge key={index} item={item} />
            ))}
          </div>
        </div>
      )}

      {/* Caveat pattern — surface limitations prominently */}
      {response.limitations.length > 0 && (
        <div className="copilot-response__limitations">
          <AlertCircle size={12} />
          <span>{response.limitations.join(' · ')}</span>
        </div>
      )}

      {/* Next steps */}
      {response.nextSteps.length > 0 && (
        <div className="copilot-response__next-steps">
          <span className="copilot-response__section-label">Next steps</span>
          <ul className="copilot-response__next-steps-list">
            {response.nextSteps.map((step, index) => (
              <li key={index}>{step}</li>
            ))}
          </ul>
        </div>
      )}

      <div className="copilot-response__footer">
        {/* Stream of Thought pattern — show which tools were called for transparency */}
        {response.toolsUsed.length > 0 && (
          <button
            className="copilot-response__tools-toggle"
            type="button"
            onClick={() => setIsToolsOpen((prev) => !prev)}
            aria-expanded={isToolsOpen}
          >
            {isToolsOpen ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
            <span>{response.toolsUsed.length} tool{response.toolsUsed.length !== 1 ? 's' : ''} used</span>
          </button>
        )}

        {/* Regenerate pattern */}
        <button
          className="copilot-response__regenerate"
          type="button"
          onClick={onRegenerate}
          aria-label="Re-run investigation"
        >
          <RefreshCw size={12} />
          <span>Re-run</span>
        </button>
      </div>

      {isToolsOpen && (
        <ul className="copilot-response__tools-list" aria-label="Tools used in this investigation">
          {response.toolsUsed.map((tool) => (
            <li key={tool}>{TOOL_DISPLAY_NAMES[tool] ?? tool}</li>
          ))}
        </ul>
      )}
    </div>
  )
}

export { CopilotResponse }
