import { useState } from 'react'
import { AlertCircle, ChevronDown, ChevronUp, RefreshCw } from 'lucide-react'
import { CopilotEvidenceBadge } from './CopilotEvidenceBadge'
import { CopilotCitedAnswer } from './CopilotCitedAnswer'
import { CopilotMeasuredEvidence } from './CopilotMeasuredEvidence'
import type { IAgentQueryResponse } from '../types/copilot.types'
import './CopilotResponse.scss'

const TOOL_DISPLAY_NAMES: Record<string, string> = {
  get_signal_metrics: 'Signal metrics',
  get_signal_findings: 'Signal findings',
  get_spectrum_summary: 'Spectrum summary',
  compare_signals: 'Compare signals',
  web_search: 'Web search',
}

interface ICopilotResponseProps {
  response: IAgentQueryResponse
  hasActivityTrace?: boolean
  onRegenerate: () => void
}

const CopilotResponse = ({ response, hasActivityTrace = false, onRegenerate }: ICopilotResponseProps) => {
  const [isToolsOpen, setIsToolsOpen] = useState(false)
  const externalCitations = response.externalCitations ?? []

  return (
    <div className="copilot-response">
      {response.evidenceSufficiency && (
        <section
          aria-label="Evidence sufficiency"
          className="copilot-response__sufficiency"
          data-status={response.evidenceSufficiency.status}
        >
          <span className="copilot-response__sufficiency-label">{response.evidenceSufficiency.label}</span>
          <span className="copilot-response__sufficiency-reason">{response.evidenceSufficiency.reason}</span>
        </section>
      )}

      {externalCitations.length > 0
        ? <CopilotCitedAnswer answer={response.answer} citations={externalCitations} />
        : <p className="copilot-response__answer">{response.answer}</p>}

      <CopilotMeasuredEvidence observations={response.structuredObservations ?? []} />

      {externalCitations.length > 0 && (
        <section className="copilot-response__section" aria-label="Sources">
          <p className="copilot-response__section-label">Sources</p>
          <ol className="copilot-response__sources">
            {externalCitations.map((citation, index) => (
              <li key={`${citation.url}-${citation.startIndex}-${citation.endIndex}`}>
                <a href={citation.url} rel="noreferrer" target="_blank">
                  <span aria-hidden="true">{index + 1}. </span>{citation.title}
                </a>
              </li>
            ))}
          </ol>
        </section>
      )}

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
        {!hasActivityTrace && response.toolsUsed.length > 0 && (
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
